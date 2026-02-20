using System;

namespace LanChatServer.Models;

public class ChatUser
{
    public string User { get; set; } = "";          // klucz (nazwa użytkownika)
    public string Machine { get; set; } = "";       // ostatnia nazwa komputera
    public bool IsOnline { get; set; }              // online/offline
    public DateTime LastSeenUtc { get; set; }        // ostatnia aktywność (register/unregister/disconnect)
}
