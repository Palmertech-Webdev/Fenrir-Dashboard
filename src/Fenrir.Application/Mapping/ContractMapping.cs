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
            source.LastSuccessfulIngestAtUtc,
            source.Config?.ToDto(),
            source.State?.ToDto(),
            source.SecretRefs.Select(secret => secret.ToDto()).ToArray(),
            source.HealthSnapshots
                .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
                .Take(10)
                .Select(snapshot => snapshot.ToDto())
                .ToArray());

    public static SiemSourceConfigDto ToDto(this SiemSourceConfig config) =>
        new(
            config.Id,
            config.SourceId,
            config.PollingIntervalSeconds,
            config.EndpointUrl,
            config.TenantId,
            config.Region,
            config.BucketName,
            config.StreamName,
            config.QueryFilter,
            config.MaxBatchSize,
            config.EnabledFromUtc,
            config.ConfigJson,
            config.CreatedAtUtc,
            config.UpdatedAtUtc);

    public static SiemSourceSecretRefDto ToDto(this SiemSourceSecretRef secretRef) =>
        new(
            secretRef.Id,
            secretRef.SourceId,
            secretRef.SecretPurpose,
            secretRef.SecretProvider,
            secretRef.SecretKey,
            secretRef.CreatedAtUtc,
            secretRef.UpdatedAtUtc);

    public static SiemSourceStateDto ToDto(this SiemSourceState state) =>
        new(
            state.Id,
            state.SourceId,
            state.ConnectorState,
            state.CursorValue,
            state.LastPollStartedAtUtc,
            state.LastPollCompletedAtUtc,
            state.LastEventTimestampUtc,
            state.NextPollAfterUtc,
            state.ConsecutiveFailureCount,
            state.LastError,
            state.CreatedAtUtc,
            state.UpdatedAtUtc);

    public static SiemSourceHealthSnapshotDto ToDto(this SiemSourceHealthSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.SourceId,
            snapshot.CapturedAtUtc,
            snapshot.Status,
            snapshot.EventsReceivedLastInterval,
            snapshot.EventsParsedLastInterval,
            snapshot.EventsFailedLastInterval,
            snapshot.ParseFailureRate,
            snapshot.LagSeconds,
            snapshot.Message);

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
