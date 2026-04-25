namespace MeGui.Models;

public class Route
{
    public Guid Id { get; set; }
    public string OriginStation { get; set; } = string.Empty;
    public string DestinationStation { get; set; } = string.Empty;

    public List<Checkpoint> Checkpoints { get; set; } = [];
}
