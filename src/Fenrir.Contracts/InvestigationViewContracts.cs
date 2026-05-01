namespace Fenrir.Contracts;

public sealed record InvestigationViewDto(
    string ViewType,
    string Title,
    string ScopeDescription,
    InvestigationViewSummaryDto Summary,
    IReadOnlyList<InvestigationPivotDto> Pivots,
    IReadOnlyList<InvestigationTimelineEventDto> Timeline,
    IReadOnlyList<InvestigationRelatedCaseDto> RelatedCases,
    IReadOnlyList<string> AnalystQuestions,
    IReadOnlyList<string> RecommendedNextActions);

public sealed record InvestigationViewSummaryDto(
    int TotalEvents,
    int HighOrCriticalEvents,
    int UniqueUsers,
    int UniqueHosts,
    int UniqueSourceIps,
    int UniqueDestinationIps,
    int UniqueDomains,
    int UniqueFileHashes,
    DateTime? FirstSeenUtc,
    DateTime? LastSeenUtc);

public sealed record InvestigationPivotDto(
    string PivotType,
    string Label,
    string Value,
    int EventCount,
    string SearchUrl);

public sealed record InvestigationTimelineEventDto(
    Guid EventId,
    DateTime TimestampUtc,
    string EventCategory,
    string EventType,
    string Severity,
    string? User,
    string Host,
    string? SourceIp,
    string? DestinationIp,
    string? Domain,
    string? Action,
    string? Outcome,
    string Message);

public sealed record InvestigationRelatedCaseDto(
    Guid CaseId,
    string CaseNumber,
    string Title,
    string Severity,
    string Status,
    DateTime UpdatedAtUtc);
