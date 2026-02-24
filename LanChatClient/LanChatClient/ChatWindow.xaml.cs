using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace LanChatClient;

public partial class ChatWindow : Window
{
    public class ChatMessage
    {
        public DateTime Time { get; set; }
        public string From { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsMine { get; set; }

        public bool IsFile { get; set; }
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string FileUrl { get; set; } = "";

        public string Meta => $"{Time:HH:mm} {From}";
        public string FileLabel => $"Pobierz: {FileName} ({FormatBytes(FileSize)})";

        private static string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB" };
            double b = bytes;
            int i = 0;
            while (b >= 1024 && i < suf.Length - 1) { b /= 1024; i++; }
            return b.ToString("0.##", CultureInfo.InvariantCulture) + " " + suf[i];
        }
    }

    private const string FileMarker = "LCFILE|";

    private readonly string _me;
    private readonly string _peer;

    private readonly Action<string, string> _send;
    private readonly Func<string, Task<int>> _deleteHistory;

    // upload -> zwraca body do pokazania (LCFILE|...)
    private readonly Func<string, string, string, Task<string>> _uploadFile;

    private readonly Action<string> _onActivated;
    private readonly Action<string> _onClosed;

    private readonly ObservableCollection<ChatMessage> _messages = new();

    public ChatWindow(
        string me,
        string peer,
        Action<string, string> send,
        Func<string, Task<int>> deleteHistory,
        Func<string, string, string, Task<string>> uploadFile,
        Action<string> onActivated,
        Action<string> onClosed)
    {
        InitializeComponent();

        _me = me;
        _peer = peer;

        _send = send;
        _deleteHistory = deleteHistory;
        _uploadFile = uploadFile;
        _onActivated = onActivated;
        _onClosed = onClosed;

        Title = $"LAN Chat - {_peer}";
        TitleText.Text = $"Rozmowa z: {_peer}";

        Activated += (_, __) => _onActivated(_peer);
        Closed += (_, __) => _onClosed(_peer);

        ChatList.ItemsSource = _messages;

        MessageText.Focus();
    }

    public void AddMessage(string from, string msg)
    {
        var cm = ParseMessage(from, msg);
        _messages.Add(cm);

        if (ChatList.Items.Count > 0)
            ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
    }

    private ChatMessage ParseMessage(string from, string msg)
    {
        var cm = new ChatMessage
        {
            Time = DateTime.Now,
            From = from,
            IsMine = from.Equals(_me, StringComparison.OrdinalIgnoreCase),
            Body = msg ?? ""
        };

        if (!string.IsNullOrEmpty(msg) && msg.StartsWith(FileMarker, StringComparison.Ordinal))
        {
            // LCFILE|name|size|url
            var parts = msg.Split('|', 4);
            if (parts.Length == 4 && long.TryParse(parts[2], out var sz))
            {
                cm.IsFile = true;
                cm.FileName = parts[1];
                cm.FileSize = sz;
                cm.FileUrl = parts[3];
                cm.Body = "";
            }
        }

        return cm;
    }

    private void Send_Click(object sender, RoutedEventArgs e) => SendNow();

    private void MessageText_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                return;

            e.Handled = true;
            SendNow();
        }
    }

    private void SendNow()
    {
        var msg = MessageText.Text ?? "";
        if (string.IsNullOrWhiteSpace(msg)) return;

        AddMessage(_me, msg);
        _send(_peer, msg);

        MessageText.Clear();
        MessageText.Focus();
    }

    private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        var res = MessageBox.Show(
            "Skasować CAŁĄ historię tej rozmowy? Operacja nieodwracalna.",
            "LanChat",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (res != MessageBoxResult.Yes) return;

        try
        {
            var deleted = await _deleteHistory(_peer);
            _messages.Clear();
            MessageBox.Show($"Usunięto {deleted} wiadomości.", "LanChat");
            MessageText.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd kasowania historii: {ex.Message}", "LanChat");
        }
    }

    private async void SendFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Wybierz plik do wysłania",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            // upload i dostajemy body (LCFILE|...)
            var body = await _uploadFile(_me, _peer, dlg.FileName);

            // pokaż u siebie od razu
            AddMessage(_me, body);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Błąd wysyłania pliku: {ex.Message}", "LanChat");
        }
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        var url = (b.Tag as string) ?? "";
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie mogę otworzyć linku: {ex.Message}", "LanChat");
        }
    }
}