using Fenrir.Contracts;
using Fenrir.Domain.Entities;

namespace Fenrir.Application.Abstractions;

public interface IEmailVerificationService
{
    Task<EmailVerificationResponse> VerifyAsync(EmailVerificationRequest request, CancellationToken cancellationToken);
}

public interface IEmailHeaderCheckService
{
    Task<EmailHeaderCheckResponse> CheckAsync(EmailHeaderCheckRequest request, CancellationToken cancellationToken);
}

public interface IIocService
{
    Task<IocCheckResponse> CheckAsync(IocCheckRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<IocRecordDto>> ImportAsync(IocImportRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<IocRecordDto>> ListAsync(CancellationToken cancellationToken);
}

public interface IDnsMonitoringService
{
    Task<DnsDomainCheckResponse> CheckDomainAsync(DnsDomainCheckRequest request, CancellationToken cancellationToken);
    Task<MonitoredDomainDto> AddMonitoredDomainAsync(MonitoredDomainRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<MonitoredDomainDto>> ListMonitoredDomainsAsync(CancellationToken cancellationToken);
}

public interface IDarkWebService
{
    Task<DarkWebCheckResponse> CheckAsync(DarkWebCheckRequest request, CancellationToken cancellationToken);
}

public interface INetworkScanningService
{
    Task<NetworkScanCreatedResponse> CreateScanAsync(NetworkScanRequest request, CancellationToken cancellationToken);
    Task<NetworkScanDto?> GetScanAsync(Guid id, CancellationToken cancellationToken);
}

public interface INetworkScanExecutor
{
    Task ExecuteAsync(Guid scanId, Guid? jobRecordId, CancellationToken cancellationToken);
}

public interface ISiemService
{
    Task<SiemEventIngestResponse> IngestAsync(SiemEventRequest request, CancellationToken cancellationToken);
    Task<SiemBatchIngestResponse> IngestBatchAsync(SiemBatchIngestRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SiemEventDto>> ListAsync(string? source, string? host, string? severity, CancellationToken cancellationToken);
    Task<IReadOnlyList<SiemEventDto>> SearchAsync(SiemEventSearchRequest request, CancellationToken cancellationToken);
    Task<SiemSourceDto> RegisterSourceAsync(SiemSourceRegistrationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SiemSourceDto>> ListSourcesAsync(CancellationToken cancellationToken);
    Task<SiemSourceDto?> GetSourceAsync(Guid id, CancellationToken cancellationToken);
    Task<SiemSourceDto?> UpdateSourceAsync(Guid id, SiemSourceUpdateRequest request, CancellationToken cancellationToken);
    Task<SiemSourceDto?> UpdateSourceConfigAsync(Guid id, SiemSourceConfigRequest request, CancellationToken cancellationToken);
    Task<SiemSourceDto?> AddOrUpdateSecretRefAsync(Guid id, SiemSourceSecretRefRequest request, CancellationToken cancellationToken);
    Task<SiemSourceDto?> RemoveSecretRefAsync(Guid id, string secretPurpose, CancellationToken cancellationToken);
    Task<SiemSourceDto?> UpdateSourceStateAsync(Guid id, SiemSourceStateRequest request, CancellationToken cancellationToken);
    Task<SiemSourceDto?> AddHealthSnapshotAsync(Guid id, SiemSourceHealthSnapshotRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<SiemIngestionJobDto>> ListIngestionJobsAsync(CancellationToken cancellationToken);
    Task<SiemIngestionJobDto?> GetIngestionJobAsync(Guid id, CancellationToken cancellationToken);
}

public interface IAgentService
{
    Task<AgentEnrolmentTokenCreatedResponse> CreateEnrolmentTokenAsync(AgentEnrolmentTokenCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentEnrolmentTokenDto>> ListEnrolmentTokensAsync(CancellationToken cancellationToken);
    Task<AgentEnrolmentTokenDto?> RevokeEnrolmentTokenAsync(Guid id, CancellationToken cancellationToken);
    Task<AgentEnrolResponse> EnrolAsync(AgentEnrolRequest request, CancellationToken cancellationToken);
    Task<AgentHeartbeatResponse?> HeartbeatAsync(string agentId, AgentHeartbeatRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AgentEndpointDto>> ListAgentsAsync(CancellationToken cancellationToken);
    Task<AgentEndpointDto?> GetAgentAsync(string agentId, CancellationToken cancellationToken);
}

public interface IFindingService
{
    Task<IReadOnlyList<FindingDto>> ListAsync(CancellationToken cancellationToken);
    Task<FindingDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<FindingDto?> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken);
}

public interface IJobService
{
    Task<IReadOnlyList<JobDto>> ListAsync(CancellationToken cancellationToken);
    Task<JobDto?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public interface IDarkWebProvider
{
    Task<DarkWebProviderResult> CheckEmailAsync(string email, CancellationToken cancellationToken);
    Task<DarkWebProviderResult> CheckDomainAsync(string domain, CancellationToken cancellationToken);
    Task<DarkWebProviderResult> CheckUsernameAsync(string username, CancellationToken cancellationToken);
}

public sealed record DarkWebProviderResult(bool Exposed, int BreachCount, IReadOnlyList<string> Sources);

public interface IDnsLookupService
{
    Task<IReadOnlyList<string>> GetARecordsAsync(string domain, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetAaaaRecordsAsync(string domain, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetMxRecordsAsync(string domain, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetTxtRecordsAsync(string domain, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetNameServersAsync(string domain, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetCaaRecordsAsync(string domain, CancellationToken cancellationToken);
    Task<bool> HasDnsSecAsync(string domain, CancellationToken cancellationToken);
}

public interface IBackgroundJobScheduler
{
    Task ScheduleNetworkScanAsync(Guid scanId, Guid jobRecordId, CancellationToken cancellationToken);
}

public interface INetworkProbe
{
    Task<PortProbeResult> ProbeAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed record PortProbeResult(bool IsOpen, string? Banner);

public interface IFenrirDataStore
{
    Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken);
    Task AddFindingAsync(Finding finding, CancellationToken cancellationToken);
    Task<IReadOnlyList<Finding>> ListFindingsAsync(CancellationToken cancellationToken);
    Task<Finding?> GetFindingAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateFindingAsync(Finding finding, CancellationToken cancellationToken);

    Task AddEmailCheckAsync(EmailCheck check, CancellationToken cancellationToken);
    Task AddEmailHeaderCheckAsync(EmailHeaderCheck check, CancellationToken cancellationToken);

    Task<Indicator?> FindIndicatorAsync(string normalizedIndicator, CancellationToken cancellationToken);
    Task<IReadOnlyList<Indicator>> FindIndicatorsAsync(IEnumerable<string> normalizedIndicators, CancellationToken cancellationToken);
    Task<IReadOnlyList<Indicator>> ListIndicatorsAsync(CancellationToken cancellationToken);
    Task UpsertIndicatorsAsync(IEnumerable<Indicator> indicators, CancellationToken cancellationToken);

    Task AddDnsCheckAsync(DnsCheck check, CancellationToken cancellationToken);
    Task<DnsCheck?> GetLatestDnsCheckAsync(string domain, CancellationToken cancellationToken);
    Task AddMonitoredDomainAsync(DnsMonitoredDomain domain, CancellationToken cancellationToken);
    Task<IReadOnlyList<DnsMonitoredDomain>> ListMonitoredDomainsAsync(CancellationToken cancellationToken);

    Task AddDarkWebCheckAsync(DarkWebCheck check, CancellationToken cancellationToken);

    Task AddNetworkScanAsync(NetworkScan scan, CancellationToken cancellationToken);
    Task UpdateNetworkScanAsync(NetworkScan scan, CancellationToken cancellationToken);
    Task<NetworkScan?> GetNetworkScanAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkScanResult>> GetNetworkScanResultsAsync(Guid scanId, CancellationToken cancellationToken);
    Task<IReadOnlyList<NetworkScanResult>> GetPreviousOpenNetworkScanResultsAsync(string target, Guid currentScanId, CancellationToken cancellationToken);
    Task AddNetworkScanResultsAsync(IEnumerable<NetworkScanResult> results, CancellationToken cancellationToken);

    Task AddSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken);

    Task AddSecurityEventsAsync(IEnumerable<SecurityEvent> securityEvents, CancellationToken cancellationToken)
    {
        return AddSecurityEventsDefaultAsync(securityEvents, cancellationToken);
    }

    private async Task AddSecurityEventsDefaultAsync(IEnumerable<SecurityEvent> securityEvents, CancellationToken cancellationToken)
    {
        foreach (var securityEvent in securityEvents)
        {
            await AddSecurityEventAsync(securityEvent, cancellationToken);
        }
    }

    Task<IReadOnlyList<SecurityEvent>> ListSecurityEventsAsync(string? source, string? host, string? severity, CancellationToken cancellationToken);

    Task<IReadOnlyList<SecurityEvent>> SearchSecurityEventsAsync(
        string? source,
        string? host,
        string? severity,
        string? eventType,
        string? userName,
        string? ipAddress,
        string? indicator,
        string? eventCategory,
        string? domain,
        string? fileHashSha256,
        string? cloudAction,
        Guid? sourceId,
        string? sourceIp,
        string? destinationIp,
        DateTime? fromUtc,
        DateTime? toUtc,
        int take,
        CancellationToken cancellationToken)
    {
        return ListSecurityEventsAsync(source, host, severity, cancellationToken);
    }

    Task AddSiemLogSourceAsync(SiemLogSource source, CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpdateSiemLogSourceAsync(SiemLogSource source, CancellationToken cancellationToken) => Task.CompletedTask;
    Task<SiemLogSource?> GetSiemLogSourceAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<SiemLogSource?>(null);
    Task<SiemLogSource?> GetSiemLogSourceByNameAsync(string name, CancellationToken cancellationToken) => Task.FromResult<SiemLogSource?>(null);
    Task<IReadOnlyList<SiemLogSource>> ListSiemLogSourcesAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SiemLogSource>>([]);
    Task UpsertSiemSourceConfigAsync(SiemSourceConfig config, CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpsertSiemSourceSecretRefAsync(SiemSourceSecretRef secretRef, CancellationToken cancellationToken) => Task.CompletedTask;
    Task RemoveSiemSourceSecretRefAsync(Guid sourceId, string secretPurpose, CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpsertSiemSourceStateAsync(SiemSourceState state, CancellationToken cancellationToken) => Task.CompletedTask;
    Task AddSiemSourceHealthSnapshotAsync(SiemSourceHealthSnapshot snapshot, CancellationToken cancellationToken) => Task.CompletedTask;
    Task AddSiemIngestionJobAsync(SiemIngestionJob job, CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpdateSiemIngestionJobAsync(SiemIngestionJob job, CancellationToken cancellationToken) => Task.CompletedTask;
    Task<SiemIngestionJob?> GetSiemIngestionJobAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<SiemIngestionJob?>(null);
    Task<IReadOnlyList<SiemIngestionJob>> ListSiemIngestionJobsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SiemIngestionJob>>([]);

    Task AddAgentEnrolmentTokenAsync(AgentEnrolmentToken token, CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpdateAgentEnrolmentTokenAsync(AgentEnrolmentToken token, CancellationToken cancellationToken) => Task.CompletedTask;
    Task<AgentEnrolmentToken?> GetAgentEnrolmentTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult<AgentEnrolmentToken?>(null);
    Task<AgentEnrolmentToken?> GetAgentEnrolmentTokenAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<AgentEnrolmentToken?>(null);
    Task<IReadOnlyList<AgentEnrolmentToken>> ListAgentEnrolmentTokensAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AgentEnrolmentToken>>([]);
    Task AddAgentEndpointAsync(AgentEndpoint agent, CancellationToken cancellationToken) => Task.CompletedTask;
    Task UpdateAgentEndpointAsync(AgentEndpoint agent, CancellationToken cancellationToken) => Task.CompletedTask;
    Task<AgentEndpoint?> GetAgentEndpointByAgentIdAsync(string agentId, CancellationToken cancellationToken) => Task.FromResult<AgentEndpoint?>(null);
    Task<AgentEndpoint?> GetAgentEndpointByMachineGuidAsync(string machineGuid, CancellationToken cancellationToken) => Task.FromResult<AgentEndpoint?>(null);
    Task<IReadOnlyList<AgentEndpoint>> ListAgentEndpointsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AgentEndpoint>>([]);

    Task AddJobAsync(JobRecord job, CancellationToken cancellationToken);
    Task<JobRecord?> GetJobAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<JobRecord>> ListJobsAsync(CancellationToken cancellationToken);
    Task UpdateJobAsync(JobRecord job, CancellationToken cancellationToken);
}
