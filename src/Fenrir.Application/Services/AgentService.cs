using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;

namespace Fenrir.Application.Services;

public sealed partial class AgentService(IFenrirDataStore dataStore) : IAgentService
{
    public async Task<AgentEnrolmentTokenCreatedResponse> CreateEnrolmentTokenAsync(AgentEnrolmentTokenCreateRequest request, CancellationToken cancellationToken)
    {
        var tokenValue = $"fenrir_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "").Replace("/", "").Replace("=", "")}";
        var token = new AgentEnrolmentToken
        {
            Name = Normalise(request.Name, "Agent enrolment token"),
            Description = request.Description,
            AllowedHostPattern = request.AllowedHostPattern,
            ExpiresAtUtc = request.ExpiresAtUtc?.ToUniversalTime(),
            MaxUses = request.MaxUses,
            TokenHash = HashToken(tokenValue)
        };

        await dataStore.AddAgentEnrolmentTokenAsync(token, cancellationToken);
        return token.ToCreatedDto(tokenValue);
    }

    public async Task<IReadOnlyList<AgentEnrolmentTokenDto>> ListEnrolmentTokensAsync(CancellationToken cancellationToken)
    {
        var tokens = await dataStore.ListAgentEnrolmentTokensAsync(cancellationToken);
        return tokens.Select(token => token.ToDto()).ToArray();
    }

    public async Task<AgentEnrolmentTokenDto?> RevokeEnrolmentTokenAsync(Guid id, CancellationToken cancellationToken)
    {
        var token = await dataStore.GetAgentEnrolmentTokenAsync(id, cancellationToken);
        if (token is null)
        {
            return null;
        }

        token.RevokedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateAgentEnrolmentTokenAsync(token, cancellationToken);
        return token.ToDto();
    }

    public async Task<AgentEnrolResponse> EnrolAsync(AgentEnrolRequest request, CancellationToken cancellationToken)
    {
        var token = await dataStore.GetAgentEnrolmentTokenByHashAsync(HashToken(request.Token), cancellationToken)
            ?? throw new InvalidOperationException("Invalid enrolment token.");

        if (!IsTokenUsable(token))
        {
            throw new InvalidOperationException("Enrolment token is expired, revoked or exhausted.");
        }

        var hostname = Normalise(request.Hostname, "unknown");
        if (!HostAllowed(hostname, token.AllowedHostPattern))
        {
            throw new InvalidOperationException("Host is not allowed by this enrolment token.");
        }

        var existing = await dataStore.GetAgentEndpointByMachineGuidAsync(request.MachineGuid, cancellationToken);
        var now = DateTime.UtcNow;
        AgentEndpoint agent;

        if (existing is null)
        {
            agent = new AgentEndpoint
            {
                AgentId = $"agent-{Guid.NewGuid():N}",
                Hostname = hostname,
                MachineGuid = Normalise(request.MachineGuid, Guid.NewGuid().ToString("N")),
                OperatingSystem = Normalise(request.OperatingSystem, "unknown"),
                AgentVersion = Normalise(request.AgentVersion, "unknown"),
                SourceId = request.SourceId,
                Status = "Healthy",
                FirstSeenAtUtc = now,
                LastSeenAtUtc = now,
                LastHeartbeatAtUtc = now,
                IpAddress = request.IpAddress,
                QueuedEventsCount = request.QueuedEventsCount,
                IsEnabled = true
            };
            await dataStore.AddAgentEndpointAsync(agent, cancellationToken);
        }
        else
        {
            agent = existing;
            ApplyAgentUpdate(agent, request.Hostname, request.MachineGuid, request.OperatingSystem, request.AgentVersion, request.IpAddress, request.SourceId, request.QueuedEventsCount, request.LastTelemetryAtUtc: null, heartbeatAtUtc: now);
            await dataStore.UpdateAgentEndpointAsync(agent, cancellationToken);
        }

        token.UseCount++;
        await dataStore.UpdateAgentEnrolmentTokenAsync(token, cancellationToken);
        return new AgentEnrolResponse(agent.ToDto());
    }

    public async Task<AgentHeartbeatResponse?> HeartbeatAsync(string agentId, AgentHeartbeatRequest request, CancellationToken cancellationToken)
    {
        var agent = await dataStore.GetAgentEndpointByAgentIdAsync(agentId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        ApplyAgentUpdate(agent, request.Hostname, request.MachineGuid, request.OperatingSystem, request.AgentVersion, request.IpAddress, request.SourceId, request.QueuedEventsCount, request.LastTelemetryAtUtc, DateTime.UtcNow);
        await dataStore.UpdateAgentEndpointAsync(agent, cancellationToken);
        return new AgentHeartbeatResponse(agent.ToDto());
    }

    public async Task<IReadOnlyList<AgentEndpointDto>> ListAgentsAsync(CancellationToken cancellationToken)
    {
        var agents = await dataStore.ListAgentEndpointsAsync(cancellationToken);
        return agents.Select(agent => agent.ToDto()).ToArray();
    }

    public async Task<AgentEndpointDto?> GetAgentAsync(string agentId, CancellationToken cancellationToken)
    {
        var agent = await dataStore.GetAgentEndpointByAgentIdAsync(agentId, cancellationToken);
        return agent?.ToDto();
    }

    private static void ApplyAgentUpdate(AgentEndpoint agent, string hostname, string machineGuid, string operatingSystem, string agentVersion, string? ipAddress, Guid? sourceId, int? queuedEventsCount, DateTime? lastTelemetryAtUtc, DateTime heartbeatAtUtc)
    {
        agent.Hostname = Normalise(hostname, agent.Hostname);
        agent.MachineGuid = Normalise(machineGuid, agent.MachineGuid);
        agent.OperatingSystem = Normalise(operatingSystem, agent.OperatingSystem);
        agent.AgentVersion = Normalise(agentVersion, agent.AgentVersion);
        agent.IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? agent.IpAddress : ipAddress.Trim();
        agent.SourceId = sourceId ?? agent.SourceId;
        agent.QueuedEventsCount = queuedEventsCount ?? agent.QueuedEventsCount;
        agent.LastTelemetryAtUtc = lastTelemetryAtUtc?.ToUniversalTime() ?? agent.LastTelemetryAtUtc;
        agent.LastSeenAtUtc = heartbeatAtUtc;
        agent.LastHeartbeatAtUtc = heartbeatAtUtc;
        agent.Status = CalculateStatus(agent);
    }

    private static string CalculateStatus(AgentEndpoint agent)
    {
        if (!agent.IsEnabled)
        {
            return "Disabled";
        }

        if (!agent.LastHeartbeatAtUtc.HasValue)
        {
            return "Unenrolled";
        }

        var age = DateTime.UtcNow - agent.LastHeartbeatAtUtc.Value;
        if (age < TimeSpan.FromMinutes(2))
        {
            return "Healthy";
        }

        return age <= TimeSpan.FromMinutes(10) ? "Warning" : "Offline";
    }

    private static bool IsTokenUsable(AgentEnrolmentToken token) =>
        token.RevokedAtUtc is null
        && (!token.ExpiresAtUtc.HasValue || token.ExpiresAtUtc.Value > DateTime.UtcNow)
        && (!token.MaxUses.HasValue || token.UseCount < token.MaxUses.Value);

    private static bool HostAllowed(string hostname, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return true;
        }

        var regex = "^" + Regex.Escape(pattern.Trim()).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(hostname, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Normalise(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
