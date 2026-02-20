using System;
using System.Windows;
using System.Windows.Input;

namespace LanChatClient;

public partial class ChatWindow : Window
{
    private readonly string _me;
    private readonly string _peer;

    private readonly Action<string, string> _send;
    private readonly Action<string> _onActivated;
    private readonly Action<string> _onClosed;

    public ChatWindow(string me, string peer,
        Action<string, string> send,
        Action<string> onActivated,
        Action<string> onClosed)
    {
        InitializeComponent();

        _me = me;
        _peer = peer;

        _send = send;
        _onActivated = onActivated;
        _onClosed = onClosed;

        Title = $"LAN Chat - {_peer}";
        TitleText.Text = $"Rozmowa z: {_peer}";

        Activated += (_, __) => _onActivated(_peer);
        Closed += (_, __) => _onClosed(_peer);

        MessageText.Focus();
    }

    public void AddMessage(string from, string msg)
    {
        ChatList.Items.Add($"{DateTime.Now:HH:mm} {from}: {msg}");
        if (ChatList.Items.Count > 0)
            ChatList.ScrollIntoView(ChatList.Items[ChatList.Items.Count - 1]);
    }

    private void Send_Click(object sender, RoutedEventArgs e)
    {
        SendNow();
    }

    private void MessageText_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendNow();
            e.Handled = true;
        }
    }

    private void SendNow()
    {
        var msg = (MessageText.Text ?? "").Trim();
        if (msg.Length == 0) return;

        AddMessage(_me, msg);
        _send(_peer, msg);

        MessageText.Clear();
        MessageText.Focus();
    }
}
