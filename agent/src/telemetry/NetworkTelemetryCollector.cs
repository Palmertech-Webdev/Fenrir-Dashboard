using System.Net.NetworkInformation;
using Fenrir.Agent.Configuration;
using Fenrir.Contracts;

namespace Fenrir.Agent.Telemetry;

public sealed class NetworkTelemetryCollector(AgentOptions options) : ITelemetryCollector
{
    private static readonly HashSet<int> RiskyPorts = [23, 445, 3389, 4444, 5900];

    private readonly HashSet<string> knownConnections = [];
    private bool baselineCaptured;

    public string Name => "network";

    public ValueTask<IReadOnlyList<SiemEventRequest>> CollectAsync(TelemetryContext context, CancellationToken cancellationToken)
    {
        var connections = IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpConnections()
            .Select(connection => new TcpConnectionSnapshot(
                connection.LocalEndPoint.Address.ToString(),
                connection.LocalEndPoint.Port,
                connection.RemoteEndPoint.Address.ToString(),
                connection.RemoteEndPoint.Port,
                connection.State.ToString()))
            .OrderBy(connection => connection.RemoteAddress)
            .ThenBy(connection => connection.RemotePort)
            .ToArray();

        if (!baselineCaptured)
        {
            baselineCaptured = true;
            foreach (var connection in connections)
            {
                knownConnections.Add(connection.Key);
            }

            if (!options.EmitNetworkInventoryOnFirstRun)
            {
                return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>([]);
            }

            var baseline = TelemetryEventFactory.Create(
                context,
                "NetworkConnectionInventory",
                "Low",
                $"Network baseline captured with {connections.Length} active TCP connection(s).",
                new
                {
                    activeTcpConnectionCount = connections.Length,
                    riskyRemotePorts = connections
                        .Where(connection => RiskyPorts.Contains(connection.RemotePort))
                        .Select(connection => connection.RemotePort)
                        .Distinct()
                        .Order()
                        .ToArray()
                });

            return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>([baseline]);
        }

        var events = new List<SiemEventRequest>();
        foreach (var connection in connections)
        {
            if (!knownConnections.Add(connection.Key))
            {
                continue;
            }

            var riskyPort = RiskyPorts.Contains(connection.RemotePort);
            events.Add(TelemetryEventFactory.Create(
                context,
                "NetworkConnectionObserved",
                riskyPort ? "Medium" : "Low",
                $"New TCP connection observed to {connection.RemoteAddress}:{connection.RemotePort}.",
                new
                {
                    connection.LocalAddress,
                    connection.LocalPort,
                    connection.RemoteAddress,
                    connection.RemotePort,
                    connection.State,
                    riskyPort
                }));

            if (events.Count >= options.BatchSize)
            {
                break;
            }
        }

        return ValueTask.FromResult<IReadOnlyList<SiemEventRequest>>(events);
    }

    private sealed record TcpConnectionSnapshot(
        string LocalAddress,
        int LocalPort,
        string RemoteAddress,
        int RemotePort,
        string State)
    {
        public string Key => $"{LocalAddress}:{LocalPort}->{RemoteAddress}:{RemotePort}:{State}";
    }
}
