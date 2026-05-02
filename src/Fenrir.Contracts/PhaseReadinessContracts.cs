namespace Fenrir.Contracts;

public sealed record PhaseReadinessDto(
    int Order,
    string Phase,
    string Title,
    string Status,
    string Summary,
    string DashboardSurface,
    IReadOnlyList<string> ApiSurfaces,
    IReadOnlyList<string> EvidenceOfCompletion,
    IReadOnlyList<string> NextHardeningItems);

public sealed record PhaseReadinessSummaryDto(
    DateTime GeneratedAtUtc,
    int TotalPhases,
    int CompletedPhases,
    int NeedsHardeningPhases,
    IReadOnlyList<PhaseReadinessDto> Phases);

public sealed record ImprovementBacklogItemDto(
    Guid Id,
    string Title,
    string Area,
    string Priority,
    string Description,
    string Status,
    DateTime CreatedAtUtc);

public sealed record CreateImprovementBacklogItemRequest(
    string Title,
    string Area,
    string Priority,
    string Description);
