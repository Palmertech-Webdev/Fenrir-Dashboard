using System.Diagnostics;
using System.Runtime.InteropServices;
using Fenrir.Contracts;

namespace Fenrir.Agent.Telemetry;

public sealed class HeartbeatTelemetryCollector : ITelemetryCollector
{
    public string Name => "heartbeat";

    public ValueTask<IReadOnlyList<SiemEventRequest>> CollectAsync(TelemetryContext context, CancellationToken cancellationToken)
    {
        using var currentProcess = Process.GetCurrentProcess();
        var ev = TelemetryEventFactory.Create(
            context,
            "AgentHeartbeat",
            "Informational",
            $"Fenrir Agent heartbeat from {context.Hostname}.",
            new
            {
                agent = "Fenrir.Agent",
                source = context.SourceName,
                host = context.Hostname,
                processId = Environment.ProcessId,
                currentProcess.WorkingSet64,
                os = RuntimeInformation.OSDescription,
                runtime = RuntimeInformation.FrameworkDescription,
                collectedAtUtc = context.CollectedAtUtc
            });

        return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>([ev]);
    }
}
