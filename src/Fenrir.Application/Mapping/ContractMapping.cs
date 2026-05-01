using Fenrir.Contracts;
using Fenrir.Domain.Entities;

namespace Fenrir.Application.Mapping;

public static class ContractMapping
{
    public static FindingDto ToDto(this Finding finding) =>
        new(
            finding.Id,
            finding.Module,
            finding.Type,
            finding.Title,
            finding.Severity,
            finding.RiskScore,
            finding.Summary,
            finding.Evidence,
            finding.Recommendation,
            finding.Status,
            finding.CreatedAtUtc,
            finding.RelatedEntityId,
            finding.RelatedEntityType);

    public static IocRecordDto ToDto(this Indicator indicator) =>
        new(
            indicator.Id,
            indicator.IndicatorValue,
            indicator.NormalizedValue,
            indicator.Type,
            indicator.Verdict,
            indicator.Severity,
            indicator.Confidence,
            indicator.Source,
            indicator.Tags,
            indicator.FirstSeenUtc,
            indicator.LastSeenUtc);

    public static MonitoredDomainDto ToDto(this DnsMonitoredDomain monitoredDomain) =>
        new(
            monitoredDomain.Id,
            monitoredDomain.Domain,
            monitoredDomain.Owner,
            monitoredDomain.IsActive,
            monitoredDomain.CreatedAtUtc);

    public static NetworkScanDto ToDto(this NetworkScan scan, IReadOnlyList<NetworkScanResult> results) =>
        new(
            scan.Id,
            scan.Target,
            scan.ScanType,
            scan.Ports,
            scan.Status,
            scan.Error,
            scan.CreatedAtUtc,
            scan.StartedAtUtc,
            scan.CompletedAtUtc,
            results.Select(ToDto).ToArray());

    public static NetworkScanResultDto ToDto(this NetworkScanResult result) =>
        new(
            result.Id,
            result.NetworkScanId,
            result.Asset,
            result.Port,
            result.IsOpen,
            result.Service,
            result.Banner,
            result.Severity,
            result.CheckedAtUtc);

    public static SiemEventDto ToDto(this SecurityEvent securityEvent) =>
        new(
            securityEvent.Id,
            securityEvent.TimestampUtc,
            securityEvent.Source,
            securityEvent.Host,
            securityEvent.EventType,
            securityEvent.Severity,
            securityEvent.Message,
            securityEvent.RawJson,
            securityEvent.IngestedAtUtc);

    public static SiemSourceDto ToDto(this SiemLogSource source) =>
        new(
            source.Id,
            source.Name,
            source.SourceType,
            source.Vendor,
            source.Product,
            source.ConnectionType,
            source.Parser,
            source.Status,
            source.Description,
            source.IsEnabled,
            source.CreatedAtUtc,
            source.UpdatedAtUtc,
            source.LastSeenAtUtc,
            source.LastSuccessfulIngestAtUtc);

    public static SiemIngestionJobDto ToDto(this SiemIngestionJob job) =>
        new(
            job.Id,
            job.SourceId,
            job.CaseId,
            job.SourceName,
            job.InputType,
            job.Parser,
            job.Status,
            job.EventsReceived,
            job.EventsParsed,
            job.EventsFailed,
            job.ErrorSummary,
            job.StartedAtUtc,
            job.CompletedAtUtc);

    public static JobDto ToDto(this JobRecord job) =>
        new(
            job.Id,
            job.JobType,
            job.Status,
            job.RelatedEntityId,
            job.RelatedEntityType,
            job.Error,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);
}
