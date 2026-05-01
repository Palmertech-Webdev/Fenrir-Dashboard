using System.Text.Json;
using Fenrir.Agent.Configuration;
using Fenrir.Contracts;

namespace Fenrir.Agent.Telemetry;

internal static class TelemetryEventFactory
{
    public static SiemEventRequest Create(
        TelemetryContext context,
        string eventType,
        string severity,
        string message,
        object raw)
    {
        var rawElement = JsonSerializer.SerializeToElement(raw, JsonOptions.Default);
        return new SiemEventRequest(
            context.CollectedAtUtc,
            context.SourceName,
            context.Hostname,
            eventType,
            severity,
            message,
            rawElement);
    }
}
