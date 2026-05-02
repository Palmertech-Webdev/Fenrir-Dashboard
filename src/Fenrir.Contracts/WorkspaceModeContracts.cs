namespace Fenrir.Contracts;

public sealed record WorkspaceModeDto(
    Guid Id,
    string UserKey,
    string Mode,
    string Role,
    string DisplayName,
    string Description,
    bool ShowAdvancedFeatures,
    bool AllowResponseActions,
    bool AllowEvidenceExports,
    bool AllowSourceConfiguration,
    DateTime UpdatedAtUtc);

public sealed record WorkspaceModeUpdateRequest(
    string Mode,
    string Role = "Analyst",
    string UserKey = "local",
    bool? ShowAdvancedFeatures = null,
    bool? AllowResponseActions = null,
    bool? AllowEvidenceExports = null,
    bool? AllowSourceConfiguration = null);

public sealed record WorkspaceModePresetDto(
    string Mode,
    string Role,
    string DisplayName,
    string Description,
    bool ShowAdvancedFeatures,
    bool AllowResponseActions,
    bool AllowEvidenceExports,
    bool AllowSourceConfiguration,
    IReadOnlyList<string> EnabledAreas,
    IReadOnlyList<string> HiddenAreas);

public sealed record FeatureAccessDto(
    string FeatureKey,
    string DisplayName,
    string Category,
    bool AnalystMode,
    bool HomeUserMode,
    bool RequiresAdvancedMode,
    string Rationale);
