using System.Diagnostics;
using Fenrir.Agent.Configuration;
using Fenrir.Contracts;

namespace Fenrir.Agent.Telemetry;

public sealed class ProcessTelemetryCollector(AgentOptions options) : ITelemetryCollector
{
    private static readonly HashSet<string> SuspiciousProcessNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "powershell",
        "pwsh",
        "cmd",
        "wscript",
        "cscript",
        "mshta",
        "rundll32",
        "regsvr32",
        "certutil",
        "bitsadmin"
    };

    private readonly HashSet<int> knownProcessIds = [];
    private bool baselineCaptured;

    public string Name => "process";

    public ValueTask<IReadOnlyList<SiemEventRequest>> CollectAsync(TelemetryContext context, CancellationToken cancellationToken)
    {
        var processes = Process.GetProcesses()
            .Select(SafeProcessSnapshot)
            .Where(process => process is not null)
            .Select(process => process!)
            .OrderBy(process => process.Id)
            .ToArray();

        if (!baselineCaptured)
        {
            baselineCaptured = true;
            foreach (var process in processes)
            {
                knownProcessIds.Add(process.Id);
            }

            if (!options.EmitProcessInventoryOnFirstRun)
            {
                return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>([]);
            }

            var baseline = TelemetryEventFactory.Create(
                context,
                "ProcessInventory",
                "Low",
                $"Process baseline captured with {processes.Length} running process(es).",
                new
                {
                    processCount = processes.Length,
                    suspiciousProcessNames = processes
                        .Where(process => SuspiciousProcessNames.Contains(process.Name))
                        .Select(process => process.Name)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Order()
                        .ToArray()
                });

            return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>([baseline]);
        }

        var events = new List<SiemEventRequest>();
        foreach (var process in processes)
        {
            if (!knownProcessIds.Add(process.Id))
            {
                continue;
            }

            var suspicious = SuspiciousProcessNames.Contains(process.Name);
            events.Add(TelemetryEventFactory.Create(
                context,
                "ProcessStarted",
                suspicious ? "Medium" : "Low",
                $"New process observed: {process.Name} ({process.Id}).",
                new
                {
                    process.Id,
                    process.Name,
                    process.StartTimeUtc,
                    suspicious
                }));

            if (events.Count >= options.BatchSize)
            {
                break;
            }
        }

        return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>(events);
    }

    private static ProcessSnapshot? SafeProcessSnapshot(Process process)
    {
        try
        {
            using (process)
            {
                DateTime? startTimeUtc = null;
                try
                {
                    startTimeUtc = process.StartTime.ToUniversalTime();
                }
                catch
                {
                    // Some protected/system processes do not allow StartTime access.
                }

                return new ProcessSnapshot(process.Id, process.ProcessName, startTimeUtc);
            }
        }
        catch
        {
            return null;
        }
    }

    private sealed record ProcessSnapshot(int Id, string Name, DateTime? StartTimeUtc);
}
