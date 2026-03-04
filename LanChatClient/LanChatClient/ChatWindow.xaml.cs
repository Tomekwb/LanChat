using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
        // UWAGA: trzymamy UTC (spójnie z serwerem), a do wyświetlania robimy ToLocalTime()
        public DateTime TimeUtc { get; set; }

        public string From { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsMine { get; set; }

        public bool IsFile { get; set; }
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string FileUrl { get; set; } = "";

        // separator dnia
        public bool IsSeparator { get; set; }
        public string SeparatorText { get; set; } = "";

        public string Meta
        {
            get
            {
                if (IsSeparator) return "";
                return $"{FormatTime(TimeUtc)} {From}";
            }
        }

        public string DisplayBody => IsSeparator ? SeparatorText : Body;

        public string FileLabel => $"Pobierz: {FileName} ({FormatBytes(FileSize)})";

        private static string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB" };
            double b = bytes;
            int i = 0;
            while (b >= 1024 && i < suf.Length - 1) { b /= 1024; i++; }
            return b.ToString("0.##", CultureInfo.InvariantCulture) + " " + suf[i];
        }

        private static string FormatTime(DateTime utc)
        {
            var local = DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();
            var now = DateTime.Now;

            if (local.Date == now.Date)
                return local.ToString("HH:mm");

            if (local.Date == now.Date.AddDays(-1))
                return "Wczoraj " + local.ToString("HH:mm");

            if (local.Year == now.Year)
                return local.ToString("dd.MM HH:mm");

            return local.ToString("dd.MM.yyyy HH:mm");
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

    // żeby separator dnia nie dublował się
    private DateTime? _lastSeparatorLocalDate = null;

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

    // kompatybilność: jak ktoś jeszcze woła AddMessage(from,msg)
    public void AddMessage(string from, string msg) => AddMessage(from, msg, DateTime.UtcNow);

    // NOWE: AddMessage z timestampem UTC (z serwera / bazy)
    public void AddMessage(string from, string msg, DateTime sentAtUtc)
    {
        AddDaySeparatorIfNeeded(sentAtUtc);

        var cm = ParseMessage(from, msg, sentAtUtc);
        _messages.Add(cm);

        if (ChatList.Items.Count > 0)
            ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
    }

    private void AddDaySeparatorIfNeeded(DateTime sentAtUtc)
    {
        var localDate = DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc).ToLocalTime().Date;

        if (_lastSeparatorLocalDate.HasValue && _lastSeparatorLocalDate.Value == localDate)
            return;

        _lastSeparatorLocalDate = localDate;

        var label = FormatDayLabel(localDate);

        _messages.Add(new ChatMessage
        {
            IsSeparator = true,
            SeparatorText = $"----- {label} -----",
            TimeUtc = sentAtUtc
        });
    }

    private static string FormatDayLabel(DateTime localDate)
    {
        var today = DateTime.Now.Date;
        if (localDate == today) return "Dzisiaj";
        if (localDate == today.AddDays(-1)) return "Wczoraj";
        if (localDate.Year == today.Year) return localDate.ToString("dd.MM");
        return localDate.ToString("dd.MM.yyyy");
    }

    private ChatMessage ParseMessage(string from, string msg, DateTime sentAtUtc)
    {
        var cm = new ChatMessage
        {
            TimeUtc = DateTime.SpecifyKind(sentAtUtc, DateTimeKind.Utc),
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

        // czas wysłania u nadawcy: UTC, żeby Meta działało spójnie
        AddMessage(_me, msg, DateTime.UtcNow);

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
            _lastSeparatorLocalDate = null;
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

            // pokaż u siebie od razu (UTC)
            AddMessage(_me, body, DateTime.UtcNow);
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
