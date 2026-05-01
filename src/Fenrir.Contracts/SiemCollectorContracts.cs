namespace Fenrir.Contracts;

public sealed record SiemBatchIngestRequest(
    string Source,
    string InputType,
    string? Parser,
    Guid? SourceId,
    Guid? CaseId,
    IReadOnlyList<SiemEventRequest> Events);

public sealed record SiemEventSearchRequest(
    string? Source = null,
    string? Host = null,
    string? Severity = null,
    string? EventType = null,
    string? UserName = null,
    string? IpAddress = null,
    string? Indicator = null,
    string? EventCategory = null,
    string? Domain = null,
    string? FileHashSha256 = null,
    string? CloudAction = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Take = 500);

public sealed record SiemSourceRegistrationRequest(
    string Name,
    string SourceType,
    string Vendor,
    string Product,
    string ConnectionType,
    string Parser,
    string? Description = null,
    bool IsEnabled = true,
    SiemSourceConfigRequest? Config = null,
    IReadOnlyList<SiemSourceSecretRefRequest>? SecretRefs = null);

public sealed record SiemSourceUpdateRequest(
    string? Name = null,
    string? SourceType = null,
    string? Vendor = null,
    string? Product = null,
    string? ConnectionType = null,
    string? Parser = null,
    string? Description = null,
    bool? IsEnabled = null,
    string? Status = null);

public sealed record SiemSourceConfigRequest(
    int? PollingIntervalSeconds = null,
    string? EndpointUrl = null,
    string? TenantId = null,
    string? Region = null,
    string? BucketName = null,
    string? StreamName = null,
    string? QueryFilter = null,
    int? MaxBatchSize = null,
    DateTime? EnabledFromUtc = null,
    string? ConfigJson = null);

public sealed record SiemSourceSecretRefRequest(
    string SecretPurpose,
    string SecretProvider,
    string SecretKey);

public sealed record SiemSourceStateRequest(
    string? ConnectorState = null,
    string? CursorValue = null,
    DateTime? LastPollStartedAtUtc = null,
    DateTime? LastPollCompletedAtUtc = null,
    DateTime? LastEventTimestampUtc = null,
    DateTime? NextPollAfterUtc = null,
    int? ConsecutiveFailureCount = null,
    string? LastError = null);

public sealed record SiemSourceHealthSnapshotRequest(
    string Status,
    int EventsReceivedLastInterval = 0,
    int EventsParsedLastInterval = 0,
    int EventsFailedLastInterval = 0,
    double ParseFailureRate = 0,
    int LagSeconds = 0,
    string? Message = null);

public sealed record SiemSourceDto(
    Guid Id,
    string Name,
    string SourceType,
    string Vendor,
    string Product,
    string ConnectionType,
    string Parser,
    string Status,
    string? Description,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? LastSeenAtUtc,
    DateTime? LastSuccessfulIngestAtUtc,
    SiemSourceConfigDto? Config = null,
    SiemSourceStateDto? State = null,
    IReadOnlyList<SiemSourceSecretRefDto>? SecretRefs = null,
    IReadOnlyList<SiemSourceHealthSnapshotDto>? RecentHealth = null);

public sealed record SiemSourceConfigDto(
    Guid Id,
    Guid SourceId,
    int PollingIntervalSeconds,
    string? EndpointUrl,
    string? TenantId,
    string? Region,
    string? BucketName,
    string? StreamName,
    string? QueryFilter,
    int MaxBatchSize,
    DateTime? EnabledFromUtc,
    string ConfigJson,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SiemSourceSecretRefDto(
    Guid Id,
    Guid SourceId,
    string SecretPurpose,
    string SecretProvider,
    string SecretKey,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SiemSourceStateDto(
    Guid Id,
    Guid SourceId,
    string ConnectorState,
    string? CursorValue,
    DateTime? LastPollStartedAtUtc,
    DateTime? LastPollCompletedAtUtc,
    DateTime? LastEventTimestampUtc,
    DateTime? NextPollAfterUtc,
    int ConsecutiveFailureCount,
    string? LastError,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SiemSourceHealthSnapshotDto(
    Guid Id,
    Guid SourceId,
    DateTime CapturedAtUtc,
    string Status,
    int EventsReceivedLastInterval,
    int EventsParsedLastInterval,
    int EventsFailedLastInterval,
    double ParseFailureRate,
    int LagSeconds,
    string? Message);

public sealed record SiemIngestionJobDto(
    Guid Id,
    Guid? SourceId,
    Guid? CaseId,
    string SourceName,
    string InputType,
    string Parser,
    string Status,
    int EventsReceived,
    int EventsParsed,
    int EventsFailed,
    string? ErrorSummary,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record SiemBatchIngestResponse(
    SiemIngestionJobDto Job,
    int EventsAccepted,
    int EventsFailed,
    IReadOnlyList<FindingDto> Findings);
