namespace MeGui.Models;

public class ChatSession
{
    public long ChatId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid? CurrentRouteId { get; set; }
    public int CurrentCheckpointOrder { get; set; }

    public Route? CurrentRoute { get; set; }
}
