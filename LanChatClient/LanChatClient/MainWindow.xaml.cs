using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;

namespace LanChatClient;

public partial class MainWindow : Window
{
    // musi pasować do serwera: ChatHub.ClientPresence
    public class ClientPresence
    {
        public string User { get; set; } = "";
        public string Machine { get; set; } = "";
        public bool IsOnline { get; set; }
        public DateTime LastSeenUtc { get; set; }

        public string Display => string.IsNullOrWhiteSpace(Machine) ? User : $"{User} ({Machine})";
        public string Status => IsOnline ? "Dostępny" : "Niedostępny";
    }

    public class HistoryItem
    {
        public string FromUser { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime SentAtUtc { get; set; }
    }

    private HubConnection? _conn;

    // UWAGA: musi się zgadzać z serwerem:
    // app.MapHub<ChatHub>("/chat");
    private const string ServerHubUrl = "http://192.168.64.233:5001/chat";

    // Auto-update (HTTP)
    private const string UpdatesBaseUrl = "http://192.168.64.233:5001/updates";
    private const string InstallDir = @"C:\LanChat";

    // Włącz na stacji testowej. Dopiero po potwierdzeniu działania — rollout na resztę.
    private const bool EnableAutoUpdate = true;

    private readonly string _cfgDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LanChat");
    private readonly string _cfgFile;

    private string _me = "";
    private readonly string _machine = Environment.MachineName;

    private readonly Dictionary<string, ChatWindow> _chatWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(DateTime ts, string from, string msg)>> _pending =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _historyLoaded = new(StringComparer.OrdinalIgnoreCase);

    private readonly LinkedList<string> _unreadQueue = new();
    private readonly HashSet<string> _unreadSet = new(StringComparer.OrdinalIgnoreCase);

    private readonly DispatcherTimer _blinkTimer = new();
    private bool _blinkState = false;

    private readonly ImageSource _iconNormal;
    private readonly ImageSource _iconAlert;

    private bool _realExit = false;

    private static string AppVersion =>
        (Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0");

    public MainWindow()
    {
        InitializeComponent();

        _cfgFile = Path.Combine(_cfgDir, "user.txt");
        Directory.CreateDirectory(_cfgDir);

        _iconNormal = new BitmapImage(new Uri("pack://application:,,,/Assets/chat.ico"));
        _iconAlert = new BitmapImage(new Uri("pack://application:,,,/Assets/chat_alert.ico"));
        TrayIcon.IconSource = _iconNormal;

        TrayIcon.TrayLeftMouseUp += (_, __) => OpenFromTray();
        TrayIcon.TrayMouseDoubleClick += (_, __) => OpenFromTray();

        _blinkTimer.Interval = TimeSpan.FromMilliseconds(500);
        _blinkTimer.Tick += (_, __) =>
        {
            _blinkState = !_blinkState;
            TrayIcon.IconSource = _blinkState ? _iconAlert : _iconNormal;
        };

        Closing += MainWindow_Closing;

        LoadOrAskName();

        // nie blokujemy UI w konstruktorze
        Loaded += async (_, __) => await StartupAsync();

        Title = $"LAN Chat v{AppVersion}";
        StatusText.Text = $"Użytkownik: {_me} | Komputer: {_machine} | v{AppVersion}";
    }

    private async System.Threading.Tasks.Task StartupAsync()
    {
        if (EnableAutoUpdate)
        {
            try
            {
                StatusText.Text = $"Sprawdzam aktualizacje... | Użytkownik: {_me} | Komputer: {_machine} | v{AppVersion}";

                var updated = await UpdateManager.CheckAndUpdateIfNeededAsync(
                    UpdatesBaseUrl,
                    InstallDir
                );

                // Jeśli updater został uruchom, klient MUSI się zamknąć,
                // inaczej pliki w InstallDir będą zablokowane.
                if (updated)
                {
                    _realExit = true;

                    try { _blinkTimer.Stop(); } catch { }
                    try { TrayIcon.Dispose(); } catch { }

                    Application.Current.Shutdown();
                    return;
                }
            }
            catch (Exception ex)
            {
                // Nie blokujemy startu chatu, jeśli update ma problem — pokaż status i idziemy dalej.
                StatusText.Text = $"AutoUpdate: błąd: {ex.Message} | Użytkownik: {_me} | Komputer: {_machine} | v{AppVersion}";
            }
        }

        StartConnection();
        await System.Threading.Tasks.Task.CompletedTask;
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
            Hide();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_realExit) return;
        e.Cancel = true;
        Hide();
    }

    private void Tray_Show_Click(object sender, RoutedEventArgs e)
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private async void Tray_Exit_Click(object sender, RoutedEventArgs e)
    {
        _realExit = true;

        try { if (_conn is not null) await _conn.InvokeAsync("Unregister"); } catch { }
        try { if (_conn is not null) await _conn.StopAsync(); } catch { }
        try { if (_conn is not null) await _conn.DisposeAsync(); } catch { }

        foreach (var w in _chatWindows.Values.ToList())
        {
            try { w.Close(); } catch { }
        }

        try { TrayIcon.Dispose(); } catch { }
        Application.Current.Shutdown();
    }

    private void OpenFromTray()
    {
        if (_unreadQueue.Count > 0)
        {
            var user = _unreadQueue.Last!.Value;
            OpenChat(user);
            return;
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void LoadOrAskName()
    {
        if (File.Exists(_cfgFile))
            _me = (File.ReadAllText(_cfgFile) ?? "").Trim();

        if (string.IsNullOrWhiteSpace(_me))
            AskAndSaveName();
    }

    private void AskAndSaveName()
    {
        var name = Microsoft.VisualBasic.Interaction.InputBox(
            "Podaj nazwę użytkownika (np. Zabiegowy, Rejestracja, Jan Kowalski).",
            "LAN Chat",
            _me.Length > 0 ? _me : "Stanowisko");

        name = (name ?? "").Trim();
        if (name.Length == 0) name = "Stanowisko";

        _me = name;
        File.WriteAllText(_cfgFile, _me);
        StatusText.Text = $"Użytkownik: {_me} | Komputer: {_machine} | v{AppVersion}";
    }

    private void ChangeName_Click(object sender, RoutedEventArgs e)
    {
        AskAndSaveName();
        MessageBox.Show("Zmieniono nazwę. Zamknij i uruchom aplikację ponownie.", "LAN Chat");
    }

    private async void StartConnection()
    {
        try
        {
            _conn = new HubConnectionBuilder()
                .WithUrl(ServerHubUrl)
                .WithAutomaticReconnect()
                .Build();

            _conn.On<List<ClientPresence>>("OnlineUsers", users =>
            {
                Dispatcher.Invoke(() =>
                {
                    UsersList.ItemsSource = users
                        .Where(u => !u.User.Equals(_me, StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(u => u.IsOnline)
                        .ThenBy(u => u.Display)
                        .ToList();
                });
            });

            _conn.On<string, string>("PrivateMessage", (from, msg) =>
{
    Dispatcher.Invoke(() =>
    {
        // Jeśli okno czatu już istnieje -> dodaj tylko do UI
        if (_chatWindows.TryGetValue(from, out var win))
        {
            win.AddMessage(from, msg);

            if (win.IsActive)
{
    ClearUnread(from);
    _pending.Remove(from); // opcjonalnie - porządek
}
            else
            {
                MarkUnread(from, $"{from}: {msg}");
            }

            // NIE zapisujemy do _pending — to powodowało duplikację
            return;
        }

        // Jeśli okna nie ma -> buforuj
        if (!_pending.TryGetValue(from, out var buf))
        {
            buf = new List<(DateTime ts, string from, string msg)>();
            _pending[from] = buf;
        }

        buf.Add((DateTime.Now, from, msg));
        MarkUnread(from, $"{from}: {msg}");
    });
});

            await _conn.StartAsync();

            // Register(user, machine) – to musi pasować do serwera
            await _conn.InvokeAsync("Register", _me, _machine);

            StatusText.Text = $"Połączono. Użytkownik: {_me} | Komputer: {_machine} | v{AppVersion}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Błąd połączenia: {ex.Message}";
        }
    }

    private void MarkUnread(string user, string balloonText)
    {
        if (!_unreadSet.Contains(user))
        {
            _unreadSet.Add(user);
            _unreadQueue.AddLast(user);
        }
        else
        {
            var node = _unreadQueue.Find(user);
            if (node != null)
            {
                _unreadQueue.Remove(node);
                _unreadQueue.AddLast(user);
            }
        }

        if (!_blinkTimer.IsEnabled)
        {
            _blinkState = false;
            _blinkTimer.Start();
        }

        TrayIcon.ShowBalloonTip("Nowa wiadomość", balloonText,
            Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
    }

    private void ClearUnread(string user)
    {
        _unreadSet.Remove(user);
        var node = _unreadQueue.Find(user);
        if (node != null) _unreadQueue.Remove(node);

        if (_unreadQueue.Count == 0)
        {
            _blinkTimer.Stop();
            TrayIcon.IconSource = _iconNormal;
        }
    }

    private async void OpenChat(string user)
    {
        if (_conn is null) return;

        if (_chatWindows.TryGetValue(user, out var existing))
        {
            if (!_historyLoaded.Contains(user))
{
    await LoadHistory(user, existing);
    _pending.Remove(user);     // kluczowe: po historii pending wyrzucamy
}
else
{
    FlushPending(user, existing); // pending ma sens tylko gdy NIE ładujesz historii
}

            existing.Show();
            existing.Activate();

            _pending.Remove(user);
            ClearUnread(user);
            return;
        }

        var win = new ChatWindow(_me, user, SendToUser,
            u => { ClearUnread(u); },
            u =>
            {
                _chatWindows.Remove(u);
                _historyLoaded.Remove(u);
            });

        _chatWindows[user] = win;

        if (!_historyLoaded.Contains(user))
{
    await LoadHistory(user, win);
    _pending.Remove(user);
}
else
{
    FlushPending(user, win);
}

        win.Show();
        win.Activate();

        _pending.Remove(user);
        ClearUnread(user);
    }

    private async System.Threading.Tasks.Task LoadHistory(string otherUser, ChatWindow win)
    {
        try
        {
            if (_conn is null) return;

            var history = await _conn.InvokeAsync<List<HistoryItem>>("GetHistory", otherUser, 50);

            foreach (var h in history)
                win.AddMessage(h.FromUser, h.Body);

            _historyLoaded.Add(otherUser);
        }
        catch { }
    }

    private void FlushPending(string user, ChatWindow win)
    {
        if (_pending.TryGetValue(user, out var buf))
        {
            foreach (var item in buf)
                win.AddMessage(item.from, item.msg);
        }
    }

    private async void SendToUser(string toUser, string message)
    {
        if (_conn is null) return;
        try { await _conn.InvokeAsync("SendPrivate", toUser, message); } catch { }
    }

    private void UsersList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (UsersList.SelectedItem is ClientPresence p)
            OpenChat(p.User);
    }
}
