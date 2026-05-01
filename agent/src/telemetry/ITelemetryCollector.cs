using Fenrir.Contracts;

namespace Fenrir.Agent.Telemetry;

public interface ITelemetryCollector
{
    string Name { get; }

    ValueTask<IReadOnlyList<SiemEventRequest>> CollectAsync(TelemetryContext context, CancellationToken cancellationToken);
}
