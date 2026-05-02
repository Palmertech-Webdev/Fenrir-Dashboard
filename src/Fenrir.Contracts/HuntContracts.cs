namespace Fenrir.Contracts;

public sealed record HuntPackCreateRequest(
    string Name,
    string Description,
    string Category = "general",
    string Severity = "Medium",
    string MitreTactic = "Discovery",
    string? MitreTechnique = null,
    bool IsEnabled = true);

public sealed record HuntQueryCreateRequest(
    string Name,
    string Description,
    string QueryType = "siem_structured",
    string QueryDefinition = "{}",
    string TargetField = "Message",
    string? ExpectedEvidence = null,
    int SortOrder = 0);

public sealed record HuntRunRequest(
    Guid HuntPackId,
    int LookbackHours = 24,
    string StartedBy = "analyst",
    string? Scope = null,
    Guid? CaseId = null);

public sealed record DfirCollectionRequest(
    string Hostname,
    string CollectionType = "triage",
    Guid? CaseId = null,
    string RequestedBy = "analyst",
    IReadOnlyList<string>? Artefacts = null,
    string? Notes = null);

public sealed record HuntPackDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Severity,
    string MitreTactic,
    string? MitreTechnique,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<HuntQueryDto> Queries);

public sealed record HuntQueryDto(
    Guid Id,
    Guid HuntPackId,
    string Name,
    string Description,
    string QueryType,
    string QueryDefinition,
    string TargetField,
    string? ExpectedEvidence,
    int SortOrder);

public sealed record HuntRunDto(
    Guid Id,
    Guid HuntPackId,
    string HuntPackName,
    string Status,
    int LookbackHours,
    string StartedBy,
    string? Scope,
    Guid? CaseId,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    int Matches,
    IReadOnlyList<HuntRunResultDto> Results);

public sealed record HuntRunResultDto(
    Guid Id,
    Guid HuntRunId,
    Guid HuntQueryId,
    string QueryName,
    Guid? EventId,
    string Severity,
    string Summary,
    string Evidence,
    DateTime CreatedAtUtc);

public sealed record DfirCollectionDto(
    Guid Id,
    string Hostname,
    string CollectionType,
    string Status,
    Guid? CaseId,
    string RequestedBy,
    IReadOnlyList<string> Artefacts,
    string? Notes,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc,
    string? EvidenceBundlePath,
    string? ErrorSummary);
