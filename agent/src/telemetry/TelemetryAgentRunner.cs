using Fenrir.Agent.Configuration;
using Fenrir.Agent.Network;
using Fenrir.Contracts;

namespace Fenrir.Agent.Telemetry;

public sealed class TelemetryAgentRunner(
    AgentOptions options,
    FenrirSiemClient client,
    IReadOnlyList<ITelemetryCollector> collectors,
    TextWriter output,
    TextWriter errorOutput)
{
    private readonly Queue<SiemEventRequest> pendingEvents = [];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        output.WriteLine($"Fenrir Agent starting. Source={options.SourceName}; API={options.ApiBaseUrl}");

        Guid? sourceId = null;
        try
        {
            var source = await client.RegisterSourceAsync(cancellationToken);
            sourceId = source?.Id;
            if (source is not null)
            {
                output.WriteLine($"Registered SIEM source: {source.Name} ({source.Id})");
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            errorOutput.WriteLine($"Source registration failed; continuing without source id. {exception.Message}");
        }

        if (options.Once)
        {
            await CollectAndSendAsync(sourceId, cancellationToken);
            return 0;
        }

        using var timer = new PeriodicTimer(options.Interval);
        while (!cancellationToken.IsCancellationRequested)
        {
            await CollectAndSendAsync(sourceId, cancellationToken);
            await timer.WaitForNextTickAsync(cancellationToken);
        }

        return 0;
    }

    private async Task CollectAndSendAsync(Guid? sourceId, CancellationToken cancellationToken)
    {
        var context = new TelemetryContext(options.SourceName, Environment.MachineName, DateTime.UtcNow);
        foreach (var collector in collectors)
        {
            try
            {
                var events = await collector.CollectAsync(context, cancellationToken);
                foreach (var ev in events)
                {
                    pendingEvents.Enqueue(ev);
                }
            }
            catch (Exception exception)
            {
                errorOutput.WriteLine($"Collector '{collector.Name}' failed: {exception.Message}");
            }
        }

        if (pendingEvents.Count == 0)
        {
            output.WriteLine("No telemetry events to send.");
            return;
        }

        var batch = DequeueBatch(options.BatchSize);
        try
        {
            var response = await client.SendBatchAsync(sourceId, batch, cancellationToken);
            output.WriteLine($"Sent {response.EventsAccepted} telemetry event(s). Findings created: {response.Findings.Count}. Job: {response.Job.Status}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            RequeueFront(batch);
            errorOutput.WriteLine($"Telemetry send failed; {pendingEvents.Count} event(s) queued locally. {exception.Message}");
        }
    }

    private IReadOnlyList<SiemEventRequest> DequeueBatch(int batchSize)
    {
        var batch = new List<SiemEventRequest>();
        while (batch.Count < batchSize && pendingEvents.TryDequeue(out var ev))
        {
            batch.Add(ev);
        }

        return batch;
    }

    private void RequeueFront(IReadOnlyList<SiemEventRequest> events)
    {
        var existing = pendingEvents.ToArray();
        pendingEvents.Clear();

        foreach (var ev in events)
        {
            pendingEvents.Enqueue(ev);
        }

        foreach (var ev in existing)
        {
            pendingEvents.Enqueue(ev);
        }
    }
}
