namespace Fenrir.Domain.Entities;

public class WorkspaceMode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserKey { get; set; } = "local";
    public string Mode { get; set; } = "Analyst";
    public string Role { get; set; } = "Analyst";
    public string DisplayName { get; set; } = "Analyst Mode";
    public string Description { get; set; } = "Full SOC investigation workspace for analysts.";
    public bool ShowAdvancedFeatures { get; set; } = true;
    public bool AllowResponseActions { get; set; } = true;
    public bool AllowEvidenceExports { get; set; } = true;
    public bool AllowSourceConfiguration { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
