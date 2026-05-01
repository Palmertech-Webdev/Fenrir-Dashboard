using Fenrir.Agent.Configuration;
using Fenrir.Agent.Network;
using Fenrir.Agent.Telemetry;

var optionsResult = AgentOptions.Load(args);
if (!optionsResult.Success)
{
    Console.Error.WriteLine(optionsResult.Error);
    AgentOptions.WriteHelp(Console.Error);
    return 2;
}

var options = optionsResult.Options;
if (options.ShowHelp)
{
    AgentOptions.WriteHelp(Console.Out);
    return 0;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

using var httpClient = new HttpClient
{
    BaseAddress = options.ApiBaseUrl,
    Timeout = TimeSpan.FromSeconds(20)
};

var client = new FenrirSiemClient(httpClient, options);
var collectors = new List<ITelemetryCollector>
{
    new HeartbeatTelemetryCollector()
};

if (options.CaptureProcesses)
{
    collectors.Add(new ProcessTelemetryCollector(options));
}

if (options.CaptureNetworkConnections)
{
    collectors.Add(new NetworkTelemetryCollector(options));
}

var runner = new TelemetryAgentRunner(options, client, collectors, Console.Out, Console.Error);
return await runner.RunAsync(cancellation.Token);
