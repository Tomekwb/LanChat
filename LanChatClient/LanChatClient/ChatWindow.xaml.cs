using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LanChatClient;

public partial class ChatWindow : Window
{
    public class ChatMessage
    {
        public DateTime Time { get; set; }
        public string From { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsMine { get; set; }
        public string Meta => $"{Time:HH:mm} {From}";
    }

    private readonly string _me;
    private readonly string _peer;

    private readonly Action<string, string> _send;
    private readonly Func<string, Task<int>> _deleteHistory;
    private readonly Action<string> _onActivated;
    private readonly Action<string> _onClosed;

    private readonly ObservableCollection<ChatMessage> _messages = new();

    public ChatWindow(
        string me,
        string peer,
        Action<string, string> send,
        Func<string, Task<int>> deleteHistory,
        Action<string> onActivated,
        Action<string> onClosed)
    {
        InitializeComponent();

        _me = me;
        _peer = peer;

        _send = send;
        _deleteHistory = deleteHistory;
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
        _messages.Add(new ChatMessage
        {
            Time = DateTime.Now,
            From = from,
            Body = msg,
            IsMine = from.Equals(_me, StringComparison.OrdinalIgnoreCase)
        });

        if (ChatList.Items.Count > 0)
            ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
    }

    private void Send_Click(object sender, RoutedEventArgs e) => SendNow();

    private void MessageText_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            // Shift+Enter -> nowa linia
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                return;

            // Enter -> wyślij
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
}