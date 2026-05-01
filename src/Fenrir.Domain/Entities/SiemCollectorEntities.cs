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

    public SiemSourceConfig? Config { get; set; }
    public SiemSourceState? State { get; set; }
    public List<SiemSourceSecretRef> SecretRefs { get; set; } = [];
    public List<SiemSourceHealthSnapshot> HealthSnapshots { get; set; } = [];
}

public class SiemSourceConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public int PollingIntervalSeconds { get; set; } = 300;
    public string? EndpointUrl { get; set; }
    public string? TenantId { get; set; }
    public string? Region { get; set; }
    public string? BucketName { get; set; }
    public string? StreamName { get; set; }
    public string? QueryFilter { get; set; }
    public int MaxBatchSize { get; set; } = 1000;
    public DateTime? EnabledFromUtc { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public SiemLogSource? Source { get; set; }
}

public class SiemSourceSecretRef
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public string SecretPurpose { get; set; } = "credential";
    public string SecretProvider { get; set; } = "LocalUserSecrets";
    public string SecretKey { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public SiemLogSource? Source { get; set; }
}

public class SiemSourceState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public string ConnectorState { get; set; } = "NotStarted";
    public string? CursorValue { get; set; }
    public DateTime? LastPollStartedAtUtc { get; set; }
    public DateTime? LastPollCompletedAtUtc { get; set; }
    public DateTime? LastEventTimestampUtc { get; set; }
    public DateTime? NextPollAfterUtc { get; set; }
    public int ConsecutiveFailureCount { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public SiemLogSource? Source { get; set; }
}

public class SiemSourceHealthSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Unknown";
    public DateTime? LastPollAtUtc { get; set; }
    public DateTime? LastSuccessfulIngestAtUtc { get; set; }
    public int EventsReceivedLastInterval { get; set; }
    public int EventsParsedLastInterval { get; set; }
    public int EventsFailedLastInterval { get; set; }
    public int EventsReceivedLast15Minutes { get; set; }
    public int EventsParsedLast15Minutes { get; set; }
    public int EventsFailedLast15Minutes { get; set; }
    public double ParseFailureRate { get; set; }
    public int AverageIngestLatencyMs { get; set; }
    public int LagSeconds { get; set; }
    public int QueueBacklog { get; set; }
    public string? LastError { get; set; }
    public string? Message { get; set; }

    public SiemLogSource? Source { get; set; }
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
