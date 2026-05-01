namespace Fenrir.Agent.Telemetry;

public sealed record TelemetryContext(string SourceName, string Hostname, DateTime CollectedAtUtc);
