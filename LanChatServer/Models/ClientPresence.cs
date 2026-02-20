namespace LanChatServer.Models;

public class ClientPresence
{
    public string User { get; set; } = "";
    public string Machine { get; set; } = "";

    public string Display => string.IsNullOrWhiteSpace(Machine) ? User : $"{User} ({Machine})";
}
