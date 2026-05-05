using System.Text.Json;

namespace Fenrir.Contracts;

public sealed record FindingDto(
    Guid Id,
    string Module,
    string Type,
    string Title,
    string Severity,
    int RiskScore,
    string Summary,
    string Evidence,
    string Recommendation,
    string Status,
    DateTime CreatedAtUtc,
    Guid? RelatedEntityId,
    string? RelatedEntityType);

public sealed record UpdateFindingStatusRequest(string Status);

public sealed record JobDto(
    Guid Id,
    string JobType,
    string Status,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    string? Error,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record EmailVerificationRequest(string Email, string? DkimSelector = null);

public sealed record EmailVerificationResponse(
    string Email,
    string Domain,
    bool FormatValid,
    bool MxPresent,
    bool SpfPresent,
    bool DmarcPresent,
    string? DkimSelector,
    bool? DkimPresent,
    bool DisposableDomain,
    string Risk,
    int TrustScore,
    string Summary,
    IReadOnlyList<FindingDto> Findings);

public sealed record EmailHeaderCheckRequest(string RawHeaders);

public sealed record EmailHeaderCheckResponse(
    string From,
    string? ReplyTo,
    string? ReturnPath,
    IReadOnlyList<string> ReceivedChain,
    IReadOnlyList<string> SendingIps,
    string? SpfResult,
    string? DkimResult,
    string? DmarcResult,
    bool FromReplyToMismatch,
    bool SuspiciousRelayChainDetected,
    bool PrivateIpLeakDetected,
    IReadOnlyList<string> HeaderUrls,
    IReadOnlyList<string> HeaderDomains,
    string Risk,
    string Summary,
    IReadOnlyList<FindingDto> Findings);

public sealed record IocCheckRequest(string? Indicator = null, IReadOnlyList<string>? Indicators = null);

public sealed record IocImportRequest(IReadOnlyList<IocImportRecord> Records);

public sealed record IocImportRecord(
    string Indicator,
    string? Type,
    string Verdict,
    string Severity,
    int Confidence,
    string Source,
    IReadOnlyList<string>? Tags = null,
    DateTime? FirstSeenUtc = null,
    DateTime? LastSeenUtc = null);

public sealed record IocRecordDto(
    Guid Id,
    string Indicator,
    string NormalizedIndicator,
    string Type,
    string Verdict,
    string Severity,
    int Confidence,
    string Source,
    IReadOnlyList<string> Tags,
    DateTime FirstSeenUtc,
    DateTime LastSeenUtc);

public sealed record IocCheckResult(
    string Indicator,
    string NormalizedIndicator,
    string Type,
    bool Matched,
    string Verdict,
    string Severity,
    int Confidence,
    string? Source,
    IReadOnlyList<string> Tags,
    FindingDto? Finding);

public sealed record IocCheckResponse(IReadOnlyList<IocCheckResult> Results);

public sealed record DnsDomainCheckRequest(string Domain);

public sealed record DnsDomainCheckResponse(
    string Domain,
    IReadOnlyList<string> ARecords,
    IReadOnlyList<string> AaaaRecords,
    IReadOnlyList<string> MxRecords,
    IReadOnlyList<string> TxtRecords,
    bool SpfPresent,
    bool DmarcPresent,
    IReadOnlyList<string> CaaRecords,
    IReadOnlyList<string> NsRecords,
    bool DnsSecAvailable,
    string Risk,
    string Summary,
    IReadOnlyList<FindingDto> Findings);

public sealed record MonitoredDomainRequest(string Domain, string? Owner = null);

public sealed record MonitoredDomainDto(Guid Id, string Domain, string? Owner, bool IsActive, DateTime CreatedAtUtc);

public sealed record DarkWebCheckRequest(string Query, string QueryType);

public sealed record DarkWebCheckResponse(
    string Query,
    string QueryType,
    bool Exposed,
    int BreachCount,
    IReadOnlyList<string> Sources,
    string Verdict,
    string Summary,
    DateTime CheckedAtUtc,
    IReadOnlyList<FindingDto> Findings);

public sealed record DarkWebExposureImportRequest(IReadOnlyList<DarkWebExposureImportItem> Items);

public sealed record DarkWebExposureImportItem(
    string Query,
    string QueryType,
    string SourceName,
    string? BreachDate = null,
    int ExposureCount = 1,
    string? Description = null);

public sealed record DarkWebExposureImportResponse(int Imported, IReadOnlyList<string> Skipped);

public sealed record NetworkScanRequest(string Target, string ScanType = "Quick", IReadOnlyList<int>? Ports = null);

public sealed record NetworkScanDto(
    Guid Id,
    string Target,
    string ScanType,
    IReadOnlyList<int> Ports,
    string Status,
    string? Error,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyList<NetworkScanResultDto> Results);

public sealed record NetworkScanResultDto(
    Guid Id,
    Guid NetworkScanId,
    string Asset,
    int Port,
    bool IsOpen,
    string? Service,
    string? Banner,
    string Severity,
    DateTime CheckedAtUtc);

public sealed record NetworkScanCreatedResponse(Guid ScanId, Guid JobId, string Status);

public sealed record SiemEventRequest(
    DateTime? Timestamp,
    string Source,
    string Host,
    string EventType,
    string Severity,
    string Message,
    JsonElement? Raw = null);

public sealed record SiemEventDto(
    Guid Id,
    DateTime TimestampUtc,
    Guid? SourceId,
    string Source,
    string? SourceName,
    string? Vendor,
    string? Product,
    string Host,
    string EventType,
    string? EventCategory,
    string Severity,
    string? User,
    string? SourceIp,
    string? DestinationIp,
    int? SourcePort,
    int? DestinationPort,
    string? Domain,
    string? Url,
    string? FileName,
    string? FilePath,
    string? FileHashSha256,
    string? ProcessName,
    string? CommandLine,
    string? ParentProcessName,
    string? Mailbox,
    string? CloudTenantId,
    string? CloudResourceId,
    string? Action,
    string? Outcome,
    string Message,
    string RawJson,
    DateTime IngestedAtUtc);

public sealed record SiemEventIngestResponse(SiemEventDto Event, IReadOnlyList<FindingDto> Findings);
