namespace Fenrir.Domain.Entities;

public class HuntPack
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "general";
    public string Severity { get; set; } = "Medium";
    public string MitreTactic { get; set; } = "Discovery";
    public string? MitreTechnique { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class HuntQuery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HuntPackId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string QueryType { get; set; } = "siem_structured";
    public string QueryDefinition { get; set; } = "{}";
    public string TargetField { get; set; } = "Message";
    public string? ExpectedEvidence { get; set; }
    public int SortOrder { get; set; }
}

public class HuntRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HuntPackId { get; set; }
    public string HuntPackName { get; set; } = "";
    public string Status { get; set; } = "Queued";
    public int LookbackHours { get; set; } = 24;
    public string StartedBy { get; set; } = "analyst";
    public string? Scope { get; set; }
    public Guid? CaseId { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int Matches { get; set; }
}

public class HuntRunResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HuntRunId { get; set; }
    public Guid HuntQueryId { get; set; }
    public string QueryName { get; set; } = "";
    public Guid? EventId { get; set; }
    public string Severity { get; set; } = "Medium";
    public string Summary { get; set; } = "";
    public string Evidence { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DfirCollection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Hostname { get; set; } = "";
    public string CollectionType { get; set; } = "triage";
    public string Status { get; set; } = "Queued";
    public Guid? CaseId { get; set; }
    public string RequestedBy { get; set; } = "analyst";
    public string ArtefactsJson { get; set; } = "[]";
    public string? Notes { get; set; }
    public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public string? EvidenceBundlePath { get; set; }
    public string? ErrorSummary { get; set; }
}
