namespace Fenrir.Contracts;

public sealed record AgentEnrolmentTokenCreateRequest(
    string Name,
    string? Description = null,
    string? AllowedHostPattern = null,
    DateTime? ExpiresAtUtc = null,
    int? MaxUses = null);

public sealed record AgentEnrolmentTokenCreatedResponse(
    Guid Id,
    string Name,
    string Token,
    string TokenPreview,
    string? Description,
    string? AllowedHostPattern,
    DateTime? ExpiresAtUtc,
    int? MaxUses,
    int UseCount,
    DateTime CreatedAtUtc,
    DateTime? RevokedAtUtc);

public sealed record AgentEnrolmentTokenDto(
    Guid Id,
    string Name,
    string TokenPreview,
    string? Description,
    string? AllowedHostPattern,
    DateTime? ExpiresAtUtc,
    int? MaxUses,
    int UseCount,
    DateTime CreatedAtUtc,
    DateTime? RevokedAtUtc,
    bool IsUsable);

public sealed record AgentEnrolRequest(
    string Token,
    string Hostname,
    string MachineGuid,
    string OperatingSystem,
    string AgentVersion,
    string? IpAddress = null,
    Guid? SourceId = null,
    int? QueuedEventsCount = null);

public sealed record AgentHeartbeatRequest(
    string Hostname,
    string MachineGuid,
    string OperatingSystem,
    string AgentVersion,
    string? IpAddress = null,
    Guid? SourceId = null,
    int? QueuedEventsCount = null,
    DateTime? LastTelemetryAtUtc = null);

public sealed record AgentEndpointDto(
    Guid Id,
    string AgentId,
    string Hostname,
    string MachineGuid,
    string OperatingSystem,
    string AgentVersion,
    Guid? SourceId,
    string Status,
    DateTime FirstSeenAtUtc,
    DateTime LastSeenAtUtc,
    DateTime? LastHeartbeatAtUtc,
    DateTime? LastTelemetryAtUtc,
    string? IpAddress,
    int? QueuedEventsCount,
    bool IsEnabled);

public sealed record AgentEnrolResponse(AgentEndpointDto Agent);

public sealed record AgentHeartbeatResponse(AgentEndpointDto Agent);
