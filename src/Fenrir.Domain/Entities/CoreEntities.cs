using Fenrir.Domain.Enums;

namespace Fenrir.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "Analyst";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string KeyHash { get; set; } = "";
    public string Role { get; set; } = "Analyst";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAtUtc { get; set; }
}

public class Asset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string AssetType { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? Hostname { get; set; }
    public string? Owner { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class AgentEnrolmentToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TokenHash { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? AllowedHostPattern { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int? MaxUses { get; set; }
    public int UseCount { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
}

public class AgentEndpoint
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AgentId { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string MachineGuid { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public string AgentVersion { get; set; } = "";
    public Guid? SourceId { get; set; }
    public string Status { get; set; } = "Unenrolled";
    public DateTime FirstSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastHeartbeatAtUtc { get; set; }
    public DateTime? LastTelemetryAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public int? QueuedEventsCount { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class Indicator
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IndicatorValue { get; set; } = "";
    public string NormalizedValue { get; set; } = "";
    public string Type { get; set; } = IndicatorTypes.Unknown;
    public string Verdict { get; set; } = IndicatorVerdicts.Unknown;
    public string Severity { get; set; } = FindingSeverity.Low;
    public int Confidence { get; set; }
    public string Source { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class Finding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Module { get; set; } = "";
    public string Type { get; set; } = "Finding";
    public string Title { get; set; } = "";
    public string Severity { get; set; } = FindingSeverity.Low;
    public int RiskScore { get; set; }
    public string Summary { get; set; } = "";
    public string Evidence { get; set; } = "";
    public string Recommendation { get; set; } = "";
    public string Status { get; set; } = FindingStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

public class EmailCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string Domain { get; set; } = "";
    public bool FormatValid { get; set; }
    public bool MxPresent { get; set; }
    public bool SpfPresent { get; set; }
    public bool DmarcPresent { get; set; }
    public string? DkimSelector { get; set; }
    public bool? DkimPresent { get; set; }
    public bool DisposableDomain { get; set; }
    public int TrustScore { get; set; }
    public string Risk { get; set; } = FindingSeverity.Low;
    public string Summary { get; set; } = "";
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class EmailHeaderCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string From { get; set; } = "";
    public string? ReplyTo { get; set; }
    public string? ReturnPath { get; set; }
    public List<string> ReceivedChain { get; set; } = [];
    public List<string> SendingIps { get; set; } = [];
    public string? SpfResult { get; set; }
    public string? DkimResult { get; set; }
    public string? DmarcResult { get; set; }
    public bool FromReplyToMismatch { get; set; }
    public bool SuspiciousRelayChainDetected { get; set; }
    public bool PrivateIpLeakDetected { get; set; }
    public List<string> HeaderUrls { get; set; } = [];
    public List<string> HeaderDomains { get; set; } = [];
    public string Risk { get; set; } = FindingSeverity.Low;
    public string Summary { get; set; } = "";
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DnsCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Domain { get; set; } = "";
    public List<string> ARecords { get; set; } = [];
    public List<string> AaaaRecords { get; set; } = [];
    public List<string> MxRecords { get; set; } = [];
    public List<string> TxtRecords { get; set; } = [];
    public List<string> CaaRecords { get; set; } = [];
    public List<string> NsRecords { get; set; } = [];
    public bool SpfPresent { get; set; }
    public bool DmarcPresent { get; set; }
    public bool DnsSecAvailable { get; set; }
    public string Risk { get; set; } = FindingSeverity.Low;
    public string Summary { get; set; } = "";
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DnsMonitoredDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Domain { get; set; } = "";
    public string? Owner { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DnsObservationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Hostname { get; set; } = "";
    public string QueriedDomain { get; set; } = "";
    public string? ResolvedIp { get; set; }
    public string Source { get; set; } = "";
    public string Verdict { get; set; } = IndicatorVerdicts.Unknown;
    public Guid? MatchedIndicatorId { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public class DarkWebCheck
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Query { get; set; } = "";
    public string QueryType { get; set; } = "";
    public bool Exposed { get; set; }
    public int BreachCount { get; set; }
    public List<string> Sources { get; set; } = [];
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class NetworkScan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Target { get; set; } = "";
    public string ScanType { get; set; } = NetworkScanTypes.Quick;
    public List<int> Ports { get; set; } = [];
    public string Status { get; set; } = JobStatus.Queued;
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class NetworkScanResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NetworkScanId { get; set; }
    public string Asset { get; set; } = "";
    public int Port { get; set; }
    public bool IsOpen { get; set; }
    public string? Service { get; set; }
    public string? Banner { get; set; }
    public string Severity { get; set; } = FindingSeverity.Informational;
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SecurityEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public Guid? SourceId { get; set; }
    public string Source { get; set; } = "";
    public string? SourceName { get; set; }
    public string? Vendor { get; set; }
    public string? Product { get; set; }
    public string Host { get; set; } = "";
    public string EventType { get; set; } = "";
    public string? EventCategory { get; set; }
    public string Severity { get; set; } = FindingSeverity.Low;
    public string? User { get; set; }
    public string? SourceIp { get; set; }
    public string? DestinationIp { get; set; }
    public int? SourcePort { get; set; }
    public int? DestinationPort { get; set; }
    public string? Domain { get; set; }
    public string? Url { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? FileHashSha256 { get; set; }
    public string? ProcessName { get; set; }
    public string? CommandLine { get; set; }
    public string? ParentProcessName { get; set; }
    public string? Mailbox { get; set; }
    public string? CloudTenantId { get; set; }
    public string? CloudResourceId { get; set; }
    public string? Action { get; set; }
    public string? Outcome { get; set; }
    public string Message { get; set; } = "";
    public string RawJson { get; set; } = "{}";
    public DateTime IngestedAtUtc { get; set; } = DateTime.UtcNow;
}

public class JobRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JobType { get; set; } = "";
    public string Status { get; set; } = JobStatus.Queued;
    public string? ExternalJobId { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Actor { get; set; } = "system";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    public string Details { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
