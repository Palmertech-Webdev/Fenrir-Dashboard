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
    public async Task<SiemEventIngestResponse> IngestAsync(SiemEventRequest request, CancellationToken cancellationToken)
    {
        var securityEvent = new SecurityEvent
        {
            TimestampUtc = request.Timestamp?.ToUniversalTime() ?? DateTime.UtcNow,
            Source = request.Source.Trim(),
            Host = request.Host.Trim(),
            EventType = request.EventType.Trim(),
            Severity = request.Severity.Trim(),
            Message = request.Message.Trim(),
            RawJson = request.Raw?.GetRawText() ?? "{}"
        };

        await dataStore.AddSecurityEventAsync(securityEvent, cancellationToken);

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

        return new SiemEventIngestResponse(securityEvent.ToDto(), findings.Select(finding => finding.ToDto()).ToArray());
    }

    public async Task<IReadOnlyList<SiemEventDto>> ListAsync(string? source, string? host, string? severity, CancellationToken cancellationToken)
    {
        var events = await dataStore.ListSecurityEventsAsync(source, host, severity, cancellationToken);
        return events.Select(securityEvent => securityEvent.ToDto()).ToArray();
    }

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
