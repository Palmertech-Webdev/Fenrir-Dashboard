namespace Fenrir.Contracts;

public sealed record CorrelationRuleCreateRequest(
    string Name,
    string Description,
    string Severity = "Medium",
    bool Enabled = true,
    string RuleType = "built_in",
    string? QueryDefinition = null,
    int TimeWindowMinutes = 60,
    string? GroupByFields = null,
    int Threshold = 3,
    string? MitreTactic = null,
    string? MitreTechnique = null);

public sealed record CorrelationRuleUpdateRequest(
    string? Name = null,
    string? Description = null,
    string? Severity = null,
    bool? Enabled = null,
    string? RuleType = null,
    string? QueryDefinition = null,
    int? TimeWindowMinutes = null,
    string? GroupByFields = null,
    int? Threshold = null,
    string? MitreTactic = null,
    string? MitreTechnique = null);

public sealed record CorrelationRunRequest(
    Guid? RuleId = null,
    int LookbackMinutes = 1440,
    int Take = 1000);

public sealed record CorrelationRuleDto(
    Guid Id,
    string Name,
    string Description,
    string Severity,
    bool Enabled,
    string RuleType,
    string QueryDefinition,
    int TimeWindowMinutes,
    string GroupByFields,
    int Threshold,
    string? MitreTactic,
    string? MitreTechnique,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CorrelationAlertDto(
    Guid Id,
    Guid? RuleId,
    string RuleName,
    string Title,
    string Description,
    string Severity,
    string Status,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<Guid> EventIds,
    IReadOnlyDictionary<string, IReadOnlyList<string>> EntitySummary,
    string? MitreTactic,
    string? MitreTechnique);

public sealed record CorrelationRunResponse(
    DateTime StartedAtUtc,
    DateTime CompletedAtUtc,
    int RulesEvaluated,
    int AlertsCreated,
    IReadOnlyList<CorrelationAlertDto> Alerts);

public sealed record EntityGraphResponse(
    IReadOnlyList<EntityGraphNodeDto> Nodes,
    IReadOnlyList<EntityGraphEdgeDto> Edges,
    IReadOnlyList<string> Narrative);

public sealed record EntityGraphNodeDto(
    string Id,
    string Label,
    string Type,
    int Weight);

public sealed record EntityGraphEdgeDto(
    string From,
    string To,
    string Relationship,
    int Weight);
