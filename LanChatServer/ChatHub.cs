using System.Collections.Concurrent;
using LanChatServer.Data;
using LanChatServer.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LanChatServer;

public class ChatHub : Hub
{
    // user -> set connectionIds (multi-conn per user)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections
        = new(StringComparer.OrdinalIgnoreCase);

    // connectionId -> user
    private static readonly ConcurrentDictionary<string, string> _connectionToUser
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly LanChatDb _db;

    public ChatHub(LanChatDb db)
    {
        _db = db;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectionToUser.TryRemove(Context.ConnectionId, out var user))
        {
            if (_userConnections.TryGetValue(user, out var conns))
            {
                conns.TryRemove(Context.ConnectionId, out _);

                if (conns.IsEmpty)
                {
                    _userConnections.TryRemove(user, out _);

                    await SetUserOnlineState(user, isOnline: false, machine: null);
                    await Clients.All.SendAsync("UserOffline", user);
                }
            }

            await BroadcastRoster();
        }

        await base.OnDisconnectedAsync(exception);
    }

    // Register(user, machine)
    public async Task Register(string user, string machine)
    {
        user = (user ?? "").Trim();
        machine = (machine ?? "").Trim();
        if (user.Length == 0) user = "Stanowisko";

        _connectionToUser[Context.ConnectionId] = user;

        var conns = _userConnections.GetOrAdd(user,
            _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        conns[Context.ConnectionId] = 0;

        // zapisz/odśwież w DB i ustaw online
        await SetUserOnlineState(user, isOnline: true, machine: machine);

        await Clients.All.SendAsync("UserOnline", user);
        await BroadcastRoster();

        // dostarcz zaległe wiadomości (trwale)
        var undelivered = await _db.Messages
            .Where(m => m.ToUser == user && m.DeliveredAtUtc == null)
            .OrderBy(m => m.SentAtUtc)
            .ToListAsync();

        if (undelivered.Count > 0)
        {
            var now = DateTime.UtcNow;

            foreach (var m in undelivered)
            {
                await Clients.Caller.SendAsync("PrivateMessage", m.FromUser, m.Body);
                m.DeliveredAtUtc = now;
            }

            await _db.SaveChangesAsync();
        }
    }

    public async Task Unregister()
    {
        if (_connectionToUser.TryRemove(Context.ConnectionId, out var user))
        {
            if (_userConnections.TryGetValue(user, out var conns))
            {
                conns.TryRemove(Context.ConnectionId, out _);

                if (conns.IsEmpty)
                {
                    _userConnections.TryRemove(user, out _);

                    await SetUserOnlineState(user, isOnline: false, machine: null);
                    await Clients.All.SendAsync("UserOffline", user);
                }
            }

            await BroadcastRoster();
        }
    }

    public async Task SendPrivate(string toUser, string message)
    {
        if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var fromUser))
            fromUser = "Nieznany";

        toUser = (toUser ?? "").Trim();
        message = (message ?? "").Trim();
        if (toUser.Length == 0 || message.Length == 0) return;

        // jeśli ktoś wysyła do usera którego nie znamy, to go rejestrujemy jako "znany kontakt" (offline)
        await EnsureUserExists(toUser);

        bool recipientOnline =
            _userConnections.TryGetValue(toUser, out var conns) &&
            conns != null &&
            !conns.IsEmpty;

        var msg = new ChatMessage
        {
            FromUser = fromUser,
            ToUser = toUser,
            Body = message,
            SentAtUtc = DateTime.UtcNow,
            DeliveredAtUtc = recipientOnline ? DateTime.UtcNow : null
        };

        _db.Messages.Add(msg);
        await _db.SaveChangesAsync();

        if (recipientOnline && conns != null)
        {
            foreach (var cid in conns.Keys)
                await Clients.Client(cid).SendAsync("PrivateMessage", fromUser, message);
        }
    }

    // Historia rozmowy (ostatnie N)
    public async Task<List<ChatMessageDto>> GetHistory(string otherUser, int take = 50)
    {
        if (!_connectionToUser.TryGetValue(Context.ConnectionId, out var me))
            me = "Nieznany";

        otherUser = (otherUser ?? "").Trim();
        if (otherUser.Length == 0) return new();

        var last = await _db.Messages
            .Where(m =>
                (m.FromUser == me && m.ToUser == otherUser) ||
                (m.FromUser == otherUser && m.ToUser == me))
            .OrderByDescending(m => m.SentAtUtc)
            .Take(take)
            .Select(m => new ChatMessageDto
            {
                FromUser = m.FromUser,
                Body = m.Body,
                SentAtUtc = m.SentAtUtc
            })
            .ToListAsync();

        last.Reverse();
        return last;
    }

    // NOWE: pełna lista kontaktów (online/offline)
    private async Task BroadcastRoster()
    {
        var users = await _db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.IsOnline)
            .ThenBy(u => u.User)
            .Select(u => new ClientPresence
            {
                User = u.User,
                Machine = u.Machine,
                IsOnline = u.IsOnline,
                LastSeenUtc = u.LastSeenUtc
            })
            .ToListAsync();

        await Clients.All.SendAsync("OnlineUsers", users);
    }

    private async Task EnsureUserExists(string user)
    {
        user = (user ?? "").Trim();
        if (user.Length == 0) return;

        var exists = await _db.Users.AnyAsync(u => u.User == user);
        if (exists) return;

        _db.Users.Add(new ChatUser
        {
            User = user,
            Machine = "",
            IsOnline = false,
            LastSeenUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private async Task SetUserOnlineState(string user, bool isOnline, string? machine)
    {
        user = (user ?? "").Trim();
        if (user.Length == 0) return;

        var now = DateTime.UtcNow;

        var u = await _db.Users.FirstOrDefaultAsync(x => x.User == user);
        if (u == null)
        {
            u = new ChatUser
            {
                User = user,
                Machine = machine ?? "",
                IsOnline = isOnline,
                LastSeenUtc = now
            };
            _db.Users.Add(u);
        }
        else
        {
            u.IsOnline = isOnline;
            u.LastSeenUtc = now;

            // machine aktualizujemy tylko gdy jest podany (przy Register)
            if (machine != null)
                u.Machine = machine;
        }

        await _db.SaveChangesAsync();
    }

    public class ChatMessageDto
    {
        public string FromUser { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime SentAtUtc { get; set; }
    }

    public class ClientPresence
    {
        public string User { get; set; } = "";
        public string Machine { get; set; } = "";
        public bool IsOnline { get; set; }
        public DateTime LastSeenUtc { get; set; }

        public string Display => string.IsNullOrWhiteSpace(Machine) ? User : $"{User} ({Machine})";
        public string Status => IsOnline ? "Dostępny" : "Niedostępny";
    }
}
