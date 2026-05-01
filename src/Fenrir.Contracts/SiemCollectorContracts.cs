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
    bool IsEnabled = true);

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
    DateTime? LastSuccessfulIngestAtUtc);

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
