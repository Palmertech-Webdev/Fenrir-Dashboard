using System.Text.Json;
using System.Text.RegularExpressions;
using Fenrir.Application.Abstractions;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;

namespace Fenrir.Application.Services;

public sealed partial class SiemLogImportService(ISiemService siemService) : ISiemLogImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SiemLogImportResponse> ImportAsync(SiemLogImportRequest request, CancellationToken cancellationToken)
    {
        var source = Normalise(request.Source, "Manual Log Import");
        var host = Normalise(request.Host, "unknown");
        var eventType = Normalise(request.EventType, "ImportedLog");
        var severity = Normalise(request.Severity, "Informational");
        var parser = Normalise(request.Parser, "generic_json_v1");
        var inputType = Normalise(request.InputType, "log_text");
        var maxEvents = Math.Clamp(request.MaxEvents, 1, 10000);

        var events = BuildEvents(request.Logs, source, host, eventType, severity, maxEvents);
        var batch = new SiemBatchIngestRequest(
            Source: source,
            InputType: inputType,
            Parser: parser,
            SourceId: request.SourceId,
            CaseId: request.CaseId,
            Events: events);

        var response = await siemService.IngestBatchAsync(batch, cancellationToken);
        return new SiemLogImportResponse(response.Job, events.Count, PreviewIndicators(events));
    }

    private static IReadOnlyList<SiemEventRequest> BuildEvents(
        string? logs,
        string source,
        string fallbackHost,
        string fallbackEventType,
        string fallbackSeverity,
        int maxEvents)
    {
        if (string.IsNullOrWhiteSpace(logs))
        {
            return [];
        }

        var trimmed = logs.Trim();
        var events = TryBuildFromJson(trimmed, source, fallbackHost, fallbackEventType, fallbackSeverity, maxEvents);
        if (events.Count > 0)
        {
            return events;
        }

        return trimmed
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(maxEvents)
            .Select(line => BuildFromTextOrJsonLine(line, source, fallbackHost, fallbackEventType, fallbackSeverity))
            .ToArray();
    }

    private static IReadOnlyList<SiemEventRequest> TryBuildFromJson(
        string input,
        string source,
        string fallbackHost,
        string fallbackEventType,
        string fallbackSeverity,
        int maxEvents)
    {
        if (!input.StartsWith('{') && !input.StartsWith('['))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return document.RootElement
                    .EnumerateArray()
                    .Take(maxEvents)
                    .Select(element => BuildFromJsonElement(element, source, fallbackHost, fallbackEventType, fallbackSeverity))
                    .ToArray();
            }

            return [BuildFromJsonElement(document.RootElement, source, fallbackHost, fallbackEventType, fallbackSeverity)];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static SiemEventRequest BuildFromTextOrJsonLine(
        string line,
        string source,
        string fallbackHost,
        string fallbackEventType,
        string fallbackSeverity)
    {
        if (line.StartsWith('{') || line.StartsWith('['))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                return BuildFromJsonElement(document.RootElement, source, fallbackHost, fallbackEventType, fallbackSeverity);
            }
            catch (JsonException)
            {
                // Treat malformed JSON log lines as plain text so analysts can still import messy exports.
            }
        }

        return new SiemEventRequest(
            Timestamp: null,
            Source: source,
            Host: fallbackHost,
            EventType: fallbackEventType,
            Severity: fallbackSeverity,
            Message: line,
            Raw: JsonSerializer.SerializeToElement(new { line }, JsonOptions));
    }

    private static SiemEventRequest BuildFromJsonElement(
        JsonElement element,
        string source,
        string fallbackHost,
        string fallbackEventType,
        string fallbackSeverity)
    {
        var message = FirstString(element, "message", "Message", "msg", "event.original", "raw", "description")
            ?? element.GetRawText();
        var host = FirstString(element, "host", "hostname", "computer", "computer_name", "device", "agent.name")
            ?? fallbackHost;
        var eventType = FirstString(element, "eventType", "event_type", "event.type", "type", "category")
            ?? fallbackEventType;
        var severity = FirstString(element, "severity", "level", "risk", "priority")
            ?? fallbackSeverity;
        var timestamp = FirstDate(element, "timestamp", "@timestamp", "time", "event.created", "created_at");

        return new SiemEventRequest(
            Timestamp: timestamp,
            Source: source,
            Host: host,
            EventType: eventType,
            Severity: severity,
            Message: message,
            Raw: element.Clone());
    }

    private static IReadOnlyList<string> PreviewIndicators(IReadOnlyList<SiemEventRequest> events)
    {
        var indicators = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var securityEvent in events.Take(100))
        {
            var text = $"{securityEvent.Message} {securityEvent.Raw?.GetRawText()}";
            foreach (Match match in IndicatorPreviewRegex().Matches(text))
            {
                var value = match.Value.TrimEnd('.', ',', ';', ')', ']');
                var classified = IndicatorClassifier.Classify(value);
                if (!string.IsNullOrWhiteSpace(classified.Normalized))
                {
                    indicators.Add(classified.Normalized);
                }

                var host = IndicatorClassifier.ExtractUrlHost(value);
                if (!string.IsNullOrWhiteSpace(host))
                {
                    indicators.Add(host);
                }
            }
        }

        return indicators.Take(50).ToArray();
    }

    private static string? FirstString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetNestedProperty(element, name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static DateTime? FirstDate(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetNestedProperty(element, name, out var value)
                && value.ValueKind == JsonValueKind.String
                && DateTime.TryParse(value.GetString(), out var timestamp))
            {
                return timestamp.ToUniversalTime();
            }
        }

        return null;
    }

    private static bool TryGetNestedProperty(JsonElement element, string dottedPath, out JsonElement value)
    {
        value = element;
        foreach (var segment in dottedPath.Split('.'))
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value))
            {
                return false;
            }
        }

        return true;
    }

    private static string Normalise(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    [GeneratedRegex(@"https?://[^\s<>'""]+|\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b|\b(?:\d{1,3}\.){3}\d{1,3}\b|\b(?:[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?\.)+[A-Z]{2,}\b|\b[A-F0-9]{32,64}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IndicatorPreviewRegex();
}
