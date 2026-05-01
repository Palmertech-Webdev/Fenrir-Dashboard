using System.Text.Json;
using System.Text.RegularExpressions;
using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed partial class SiemService(IFenrirDataStore dataStore) : ISiemService
{
    private const string DefaultParser = "generic_json_v1";

    public async Task<SiemEventIngestResponse> IngestAsync(SiemEventRequest request, CancellationToken cancellationToken)
    {
        var securityEvent = BuildSecurityEvent(request, request.Source, DefaultParser);
        await dataStore.AddSecurityEventAsync(securityEvent, cancellationToken);

        var findings = await CreateFindingsForEventAsync(securityEvent, cancellationToken);
        return new SiemEventIngestResponse(securityEvent.ToDto(), findings.Select(finding => finding.ToDto()).ToArray());
    }

    public async Task<SiemBatchIngestResponse> IngestBatchAsync(SiemBatchIngestRequest request, CancellationToken cancellationToken)
    {
        var sourceName = string.IsNullOrWhiteSpace(request.Source) ? "Manual Upload" : request.Source.Trim();
        var parser = string.IsNullOrWhiteSpace(request.Parser) ? DefaultParser : request.Parser.Trim();

        var job = new SiemIngestionJob
        {
            SourceId = request.SourceId,
            CaseId = request.CaseId,
            SourceName = sourceName,
            InputType = string.IsNullOrWhiteSpace(request.InputType) ? "json" : request.InputType.Trim(),
            Parser = parser,
            Status = "processing",
            EventsReceived = request.Events.Count,
            StartedAtUtc = DateTime.UtcNow
        };

        await dataStore.AddSiemIngestionJobAsync(job, cancellationToken);

        var acceptedEvents = new List<SecurityEvent>();
        var findings = new List<Finding>();
        var failed = 0;

        foreach (var eventRequest in request.Events)
        {
            try
            {
                var securityEvent = BuildSecurityEvent(eventRequest, sourceName, parser);
                acceptedEvents.Add(securityEvent);
            }
            catch
            {
                failed++;
            }
        }

        if (acceptedEvents.Count > 0)
        {
            await dataStore.AddSecurityEventsAsync(acceptedEvents, cancellationToken);

            foreach (var securityEvent in acceptedEvents)
            {
                findings.AddRange(await CreateFindingsForEventAsync(securityEvent, cancellationToken));
            }
        }

        if (request.SourceId.HasValue)
        {
            var source = await dataStore.GetSiemLogSourceAsync(request.SourceId.Value, cancellationToken);
            if (source is not null)
            {
                source.LastSeenAtUtc = DateTime.UtcNow;
                source.LastSuccessfulIngestAtUtc = acceptedEvents.Count > 0 ? DateTime.UtcNow : source.LastSuccessfulIngestAtUtc;
                source.Status = failed == 0 ? "Healthy" : "Warning";
                source.UpdatedAtUtc = DateTime.UtcNow;
                await dataStore.UpdateSiemLogSourceAsync(source, cancellationToken);
            }
        }

        job.EventsParsed = acceptedEvents.Count;
        job.EventsFailed = failed;
        job.Status = failed == 0 ? "completed" : acceptedEvents.Count > 0 ? "partially_parsed" : "failed";
        job.ErrorSummary = failed == 0 ? null : $"{failed} event(s) could not be parsed or normalised.";
        job.CompletedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateSiemIngestionJobAsync(job, cancellationToken);

        return new SiemBatchIngestResponse(job.ToDto(), acceptedEvents.Count, failed, findings.Select(finding => finding.ToDto()).ToArray());
    }

    public async Task<IReadOnlyList<SiemEventDto>> ListAsync(string? source, string? host, string? severity, CancellationToken cancellationToken)
    {
        var events = await dataStore.ListSecurityEventsAsync(source, host, severity, cancellationToken);
        return events.Select(securityEvent => securityEvent.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<SiemEventDto>> SearchAsync(SiemEventSearchRequest request, CancellationToken cancellationToken)
    {
        var events = await dataStore.SearchSecurityEventsAsync(
            request.Source,
            request.Host,
            request.Severity,
            request.EventType,
            request.UserName,
            request.IpAddress,
            request.Indicator,
            request.FromUtc,
            request.ToUtc,
            request.Take,
            cancellationToken);

        return events.Select(securityEvent => securityEvent.ToDto()).ToArray();
    }

    public async Task<SiemSourceDto> RegisterSourceAsync(SiemSourceRegistrationRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var existing = await dataStore.GetSiemLogSourceByNameAsync(name, cancellationToken);

        if (existing is null)
        {
            var source = new SiemLogSource
            {
                Name = name,
                SourceType = NormaliseOrDefault(request.SourceType, "manual_upload"),
                Vendor = NormaliseOrDefault(request.Vendor, "generic"),
                Product = NormaliseOrDefault(request.Product, "generic"),
                ConnectionType = NormaliseOrDefault(request.ConnectionType, "manual"),
                Parser = NormaliseOrDefault(request.Parser, DefaultParser),
                Description = request.Description,
                IsEnabled = request.IsEnabled,
                Status = request.IsEnabled ? "Healthy" : "Disabled"
            };

            await dataStore.AddSiemLogSourceAsync(source, cancellationToken);
            return source.ToDto();
        }

        existing.SourceType = NormaliseOrDefault(request.SourceType, existing.SourceType);
        existing.Vendor = NormaliseOrDefault(request.Vendor, existing.Vendor);
        existing.Product = NormaliseOrDefault(request.Product, existing.Product);
        existing.ConnectionType = NormaliseOrDefault(request.ConnectionType, existing.ConnectionType);
        existing.Parser = NormaliseOrDefault(request.Parser, existing.Parser);
        existing.Description = request.Description;
        existing.IsEnabled = request.IsEnabled;
        existing.Status = request.IsEnabled ? existing.Status == "Disabled" ? "Healthy" : existing.Status : "Disabled";
        existing.UpdatedAtUtc = DateTime.UtcNow;
        await dataStore.UpdateSiemLogSourceAsync(existing, cancellationToken);
        return existing.ToDto();
    }

    public async Task<IReadOnlyList<SiemSourceDto>> ListSourcesAsync(CancellationToken cancellationToken)
    {
        var sources = await dataStore.ListSiemLogSourcesAsync(cancellationToken);
        return sources.Select(source => source.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<SiemIngestionJobDto>> ListIngestionJobsAsync(CancellationToken cancellationToken)
    {
        var jobs = await dataStore.ListSiemIngestionJobsAsync(cancellationToken);
        return jobs.Select(job => job.ToDto()).ToArray();
    }

    public async Task<SiemIngestionJobDto?> GetIngestionJobAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dataStore.GetSiemIngestionJobAsync(id, cancellationToken);
        return job?.ToDto();
    }

    private static SecurityEvent BuildSecurityEvent(SiemEventRequest request, string fallbackSource, string parser)
    {
        return new SecurityEvent
        {
            TimestampUtc = request.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow,
            Source = string.IsNullOrWhiteSpace(request.Source) ? fallbackSource : request.Source.Trim(),
            Host = string.IsNullOrWhiteSpace(request.Host) ? "unknown" : request.Host.Trim(),
            EventType = string.IsNullOrWhiteSpace(request.EventType) ? "generic_event" : request.EventType.Trim(),
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? FindingSeverity.Low : request.Severity.Trim(),
            Message = request.Message?.Trim() ?? string.Empty,
            RawJson = request.Raw?.GetRawText() ?? "{}",
            IngestedAtUtc = DateTime.UtcNow
        };
    }

    private async Task<IReadOnlyList<Finding>> CreateFindingsForEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        var findings = new List<Finding>();
        if (IsHighSeverity(securityEvent.Severity))
        {
            findings.Add(new Finding
            {
                Module = "SIEM",
                Type = "SiemFinding",
                Title = $"High-severity event received: {securityEvent.EventType}",
                Severity = securityEvent.Severity,
                RiskScore = SecurityHelpers.SeverityWeight(securityEvent.Severity),
                Summary = securityEvent.Message,
                Evidence = $"Source={securityEvent.Source}; Host={securityEvent.Host}; EventType={securityEvent.EventType}",
                Recommendation = "Review the event, identify related assets and IOCs, and open an incident workflow if confirmed.",
                RelatedEntityId = securityEvent.Id,
                RelatedEntityType = nameof(SecurityEvent)
            });
        }

        var extractedIndicators = ExtractPotentialIndicators(securityEvent.Message + " " + securityEvent.RawJson);
        foreach (var indicator in extractedIndicators)
        {
            var match = await dataStore.FindIndicatorAsync(indicator.Normalized, cancellationToken);
            if (match is null || !IsFindingWorthy(match))
            {
                continue;
            }

            findings.Add(new Finding
            {
                Module = "SIEM",
                Type = "SiemFinding",
                Title = "Security event contains matched IOC",
                Severity = match.Severity,
                RiskScore = SecurityHelpers.SeverityWeight(match.Severity),
                Summary = $"{indicator.Original} in event matched local IOC storage.",
                Evidence = $"Indicator={match.IndicatorValue}; Source={match.Source}; EventId={securityEvent.Id}",
                Recommendation = "Pivot from this event into related endpoint, DNS, and identity telemetry.",
                RelatedEntityId = securityEvent.Id,
                RelatedEntityType = nameof(SecurityEvent)
            });
        }

        foreach (var finding in findings)
        {
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }

        return findings;
    }

    private static string NormaliseOrDefault(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static bool IsHighSeverity(string severity) =>
        string.Equals(severity, FindingSeverity.High, StringComparison.OrdinalIgnoreCase)
        || string.Equals(severity, FindingSeverity.Critical, StringComparison.OrdinalIgnoreCase);

    private static bool IsFindingWorthy(Indicator indicator) =>
        string.Equals(indicator.Verdict, IndicatorVerdicts.Malicious, StringComparison.OrdinalIgnoreCase)
        || string.Equals(indicator.Verdict, IndicatorVerdicts.Suspicious, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<ClassifiedIndicator> ExtractPotentialIndicators(string text)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in UrlRegex().Matches(text))
        {
            candidates.Add(match.Value.TrimEnd('.', ',', ';', ')', ']'));
        }

        foreach (Match match in IpRegex().Matches(text))
        {
            candidates.Add(match.Value);
        }

        foreach (Match match in EmailRegex().Matches(text))
        {
            candidates.Add(match.Value);
        }

        foreach (Match match in DomainRegex().Matches(text))
        {
            candidates.Add(match.Value.TrimEnd('.'));
        }

        return candidates.Select(IndicatorClassifier.Classify).Where(indicator => !string.IsNullOrWhiteSpace(indicator.Normalized));
    }

    [GeneratedRegex(@"https?://[^\s<>'""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex IpRegex();

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?\.)+[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();
}
