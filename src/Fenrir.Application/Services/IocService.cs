using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed class IocService(IFenrirDataStore dataStore) : IIocService
{
    public async Task<IocCheckResponse> CheckAsync(IocCheckRequest request, CancellationToken cancellationToken)
    {
        var submitted = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Indicator))
        {
            submitted.Add(request.Indicator);
        }

        if (request.Indicators is not null)
        {
            submitted.AddRange(request.Indicators.Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        var results = new List<IocCheckResult>();
        foreach (var value in submitted.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var classified = IndicatorClassifier.Classify(value);
            var match = string.IsNullOrWhiteSpace(classified.Normalized)
                ? null
                : await dataStore.FindIndicatorAsync(classified.Normalized, cancellationToken);

            Finding? finding = null;
            if (match is not null && IsFindingWorthy(match))
            {
                finding = new Finding
                {
                    Module = "IOC",
                    Type = "IocFinding",
                    Title = $"IOC matched {match.Verdict.ToLowerInvariant()} indicator",
                    Severity = match.Severity,
                    RiskScore = SecurityHelpers.SeverityWeight(match.Severity),
                    Summary = $"{classified.Original} matched local IOC storage.",
                    Evidence = $"Indicator={match.IndicatorValue}; Source={match.Source}; Confidence={match.Confidence}",
                    Recommendation = "Triage the related asset, user, message, or event and preserve evidence before remediation.",
                    RelatedEntityId = match.Id,
                    RelatedEntityType = nameof(Indicator)
                };
                await dataStore.AddFindingAsync(finding, cancellationToken);
            }

            results.Add(new IocCheckResult(
                classified.Original,
                classified.Normalized,
                classified.Type,
                match is not null,
                match?.Verdict ?? IndicatorVerdicts.Unknown,
                match?.Severity ?? FindingSeverity.Informational,
                match?.Confidence ?? 0,
                match?.Source,
                match?.Tags ?? [],
                finding?.ToDto()));
        }

        return new IocCheckResponse(results);
    }

    public async Task<IReadOnlyList<IocRecordDto>> ImportAsync(IocImportRequest request, CancellationToken cancellationToken)
    {
        var indicators = request.Records
            .Where(record => !string.IsNullOrWhiteSpace(record.Indicator))
            .Select(record =>
            {
                var classified = IndicatorClassifier.Classify(record.Indicator);
                return new Indicator
                {
                    IndicatorValue = record.Indicator.Trim(),
                    NormalizedValue = classified.Normalized,
                    Type = string.IsNullOrWhiteSpace(record.Type) ? classified.Type : record.Type.Trim(),
                    Verdict = string.IsNullOrWhiteSpace(record.Verdict) ? IndicatorVerdicts.Unknown : record.Verdict.Trim(),
                    Severity = string.IsNullOrWhiteSpace(record.Severity) ? FindingSeverity.Low : record.Severity.Trim(),
                    Confidence = Math.Clamp(record.Confidence, 0, 100),
                    Source = record.Source.Trim(),
                    Tags = record.Tags?.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? [],
                    FirstSeenUtc = record.FirstSeenUtc ?? DateTime.UtcNow,
                    LastSeenUtc = record.LastSeenUtc ?? DateTime.UtcNow
                };
            })
            .Where(indicator => !string.IsNullOrWhiteSpace(indicator.NormalizedValue))
            .ToArray();

        await dataStore.UpsertIndicatorsAsync(indicators, cancellationToken);
        return indicators.Select(indicator => indicator.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<IocRecordDto>> ListAsync(CancellationToken cancellationToken)
    {
        var indicators = await dataStore.ListIndicatorsAsync(cancellationToken);
        return indicators.Select(indicator => indicator.ToDto()).ToArray();
    }

    private static bool IsFindingWorthy(Indicator indicator) =>
        string.Equals(indicator.Verdict, IndicatorVerdicts.Malicious, StringComparison.OrdinalIgnoreCase)
        || string.Equals(indicator.Verdict, IndicatorVerdicts.Suspicious, StringComparison.OrdinalIgnoreCase);
}
