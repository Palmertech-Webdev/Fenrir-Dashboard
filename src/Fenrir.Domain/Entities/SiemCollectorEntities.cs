namespace Fenrir.Domain.Entities;

public class SiemLogSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string SourceType { get; set; } = "manual_upload";
    public string Vendor { get; set; } = "generic";
    public string Product { get; set; } = "generic";
    public string ConnectionType { get; set; } = "manual";
    public string Parser { get; set; } = "generic_json_v1";
    public string Status { get; set; } = "Healthy";
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime? LastSuccessfulIngestAtUtc { get; set; }
}

public class SiemIngestionJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SourceId { get; set; }
    public Guid? CaseId { get; set; }
    public string SourceName { get; set; } = "";
    public string InputType { get; set; } = "json";
    public string Parser { get; set; } = "generic_json_v1";
    public string Status { get; set; } = "received";
    public int EventsReceived { get; set; }
    public int EventsParsed { get; set; }
    public int EventsFailed { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
