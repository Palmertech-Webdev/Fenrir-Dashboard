namespace Fenrir.Contracts;

public sealed record ResponsePlaybookCreateRequest(
    string Name,
    string Description,
    string Category = "general",
    string Severity = "Medium",
    string TriggerType = "manual",
    string? MitreTactic = null,
    string? MitreTechnique = null,
    bool IsEnabled = true);

public sealed record ResponsePlaybookStepCreateRequest(
    string Title,
    string Description,
    string ActionType = "manual",
    string TargetType = "analyst",
    string? CommandPreview = null,
    string? IntegrationKey = null,
    bool RequiresApproval = true,
    int SortOrder = 0);

public sealed record ResponsePlaybookRunRequest(
    Guid PlaybookId,
    Guid? CaseId = null,
    Guid? AlertId = null,
    Guid? EventId = null,
    string StartedBy = "analyst",
    string? Notes = null);

public sealed record ResponsePlaybookStepUpdateRequest(
    string Status,
    string? Result = null,
    string? ExecutedBy = null);

public sealed record ResponsePlaybookDto(
    Guid Id,
    string Name,
    string Description,
    string Category,
    string Severity,
    string TriggerType,
    string? MitreTactic,
    string? MitreTechnique,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<ResponsePlaybookStepDto> Steps);

public sealed record ResponsePlaybookStepDto(
    Guid Id,
    Guid PlaybookId,
    string Title,
    string Description,
    string ActionType,
    string TargetType,
    string? CommandPreview,
    string? IntegrationKey,
    bool RequiresApproval,
    int SortOrder);

public sealed record ResponsePlaybookRunDto(
    Guid Id,
    Guid PlaybookId,
    string PlaybookName,
    Guid? CaseId,
    Guid? AlertId,
    Guid? EventId,
    string Status,
    string StartedBy,
    string? Notes,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<ResponsePlaybookRunStepDto> Steps);

public sealed record ResponsePlaybookRunStepDto(
    Guid Id,
    Guid RunId,
    Guid PlaybookStepId,
    string Title,
    string Status,
    string? Result,
    string? ExecutedBy,
    DateTime? ExecutedAtUtc,
    bool RequiresApproval,
    int SortOrder);

public sealed record ResponseRecommendationRequest(
    Guid? AlertId = null,
    Guid? CaseId = null,
    Guid? EventId = null);

public sealed record ResponseRecommendationDto(
    string Title,
    string Rationale,
    string Severity,
    IReadOnlyList<Guid> RecommendedPlaybookIds,
    IReadOnlyList<string> RecommendedActions);
