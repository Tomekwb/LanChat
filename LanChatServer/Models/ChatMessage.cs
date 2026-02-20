using System;

namespace LanChatServer.Models;

public class ChatMessage
{
    public long Id { get; set; }

    public string FromUser { get; set; } = "";
    public string ToUser { get; set; } = "";

    public string Body { get; set; } = "";

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeliveredAtUtc { get; set; }
}
