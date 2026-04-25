namespace MeGui.Models;

public class Checkpoint
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public int Order { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;

    public Route Route { get; set; } = null!;
}
