namespace Fenrir.Domain.Entities;

public class ImprovementBacklogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Area { get; set; } = "General";
    public string Priority { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
