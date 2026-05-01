namespace Fenrir.Domain.Entities;

public class CorrelationRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "Medium";
    public bool Enabled { get; set; } = true;
    public string RuleType { get; set; } = "built_in";
    public string QueryDefinition { get; set; } = "";
    public int TimeWindowMinutes { get; set; } = 60;
    public string GroupByFields { get; set; } = "";
    public int Threshold { get; set; } = 3;
    public string? MitreTactic { get; set; }
    public string? MitreTechnique { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class CorrelationAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RuleId { get; set; }
    public string RuleName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string EventIdsJson { get; set; } = "[]";
    public string EntitySummaryJson { get; set; } = "{}";
    public string? MitreTactic { get; set; }
    public string? MitreTechnique { get; set; }
}
