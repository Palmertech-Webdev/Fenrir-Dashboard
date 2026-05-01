using System.Text.Json;

namespace Fenrir.Agent.Configuration;

public sealed class AgentOptions
{
    public Uri ApiBaseUrl { get; set; } = new("http://localhost:5248");
    public string SourceName { get; set; } = $"FenrirAgent-{Environment.MachineName}";
    public string? BearerToken { get; set; }
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);
    public int BatchSize { get; set; } = 100;
    public bool RegisterSource { get; set; } = true;
    public bool CaptureProcesses { get; set; } = true;
    public bool CaptureNetworkConnections { get; set; } = true;
    public bool EmitProcessInventoryOnFirstRun { get; set; } = true;
    public bool EmitNetworkInventoryOnFirstRun { get; set; } = true;
    public bool Once { get; set; }
    public bool ShowHelp { get; set; }

    public static AgentOptionsLoadResult Load(string[] args)
    {
        var options = LoadFromConfigFile();
        ApplyEnvironment(options);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--api":
                    if (!TryReadValue(args, ref i, out var api) || !Uri.TryCreate(api, UriKind.Absolute, out var apiUri))
                    {
                        return AgentOptionsLoadResult.Fail("--api requires an absolute URL.");
                    }

                    options.ApiBaseUrl = apiUri;
                    break;
                case "--source":
                    if (!TryReadValue(args, ref i, out var source))
                    {
                        return AgentOptionsLoadResult.Fail("--source requires a value.");
                    }

                    options.SourceName = source;
                    break;
                case "--token":
                    if (!TryReadValue(args, ref i, out var token))
                    {
                        return AgentOptionsLoadResult.Fail("--token requires a value.");
                    }

                    options.BearerToken = token;
                    break;
                case "--interval":
                    if (!TryReadValue(args, ref i, out var interval) || !int.TryParse(interval, out var intervalSeconds) || intervalSeconds < 5)
                    {
                        return AgentOptionsLoadResult.Fail("--interval requires a whole number of at least 5 seconds.");
                    }

                    options.Interval = TimeSpan.FromSeconds(intervalSeconds);
                    break;
                case "--batch-size":
                    if (!TryReadValue(args, ref i, out var batchSize) || !int.TryParse(batchSize, out var batchSizeValue) || batchSizeValue < 1)
                    {
                        return AgentOptionsLoadResult.Fail("--batch-size requires a positive whole number.");
                    }

                    options.BatchSize = Math.Clamp(batchSizeValue, 1, 1000);
                    break;
                case "--once":
                    options.Once = true;
                    break;
                case "--no-register":
                    options.RegisterSource = false;
                    break;
                case "--no-process":
                    options.CaptureProcesses = false;
                    break;
                case "--no-network":
                    options.CaptureNetworkConnections = false;
                    break;
                default:
                    return AgentOptionsLoadResult.Fail($"Unknown argument: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(options.SourceName))
        {
            options.SourceName = $"FenrirAgent-{Environment.MachineName}";
        }

        return AgentOptionsLoadResult.Ok(options);
    }

    public static void WriteHelp(TextWriter writer)
    {
        writer.WriteLine("Fenrir Agent");
        writer.WriteLine("Usage: dotnet run --project agent/Fenrir.Agent.csproj -- [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --api <url>             Fenrir API base URL. Default: http://localhost:5248");
        writer.WriteLine("  --source <name>         SIEM source name. Default: FenrirAgent-{machine}");
        writer.WriteLine("  --token <jwt>           Optional bearer token.");
        writer.WriteLine("  --interval <seconds>    Collection interval. Minimum: 5. Default: 30.");
        writer.WriteLine("  --batch-size <count>    Max events per batch. Default: 100.");
        writer.WriteLine("  --once                  Collect once, send once, then exit.");
        writer.WriteLine("  --no-process            Disable process telemetry.");
        writer.WriteLine("  --no-network            Disable network telemetry.");
        writer.WriteLine("  --no-register           Skip SIEM source registration.");
    }

    private static AgentOptions LoadFromConfigFile()
    {
        foreach (var path in CandidateConfigPaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var config = JsonSerializer.Deserialize<AgentOptionsFile>(File.ReadAllText(path), JsonOptions.Default);
                return config?.ToOptions() ?? new AgentOptions();
            }
            catch (JsonException)
            {
                return new AgentOptions();
            }
        }

        return new AgentOptions();
    }

    private static IEnumerable<string> CandidateConfigPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        yield return Path.Combine(Environment.CurrentDirectory, "agent", "appsettings.json");
        yield return Path.Combine(Environment.CurrentDirectory, "appsettings.json");
    }

    private static void ApplyEnvironment(AgentOptions options)
    {
        if (Uri.TryCreate(Environment.GetEnvironmentVariable("FENRIR_API_URL"), UriKind.Absolute, out var apiUrl))
        {
            options.ApiBaseUrl = apiUrl;
        }

        var sourceName = Environment.GetEnvironmentVariable("FENRIR_AGENT_NAME");
        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            options.SourceName = sourceName;
        }

        var token = Environment.GetEnvironmentVariable("FENRIR_AGENT_TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
        {
            options.BearerToken = token;
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("FENRIR_AGENT_INTERVAL_SECONDS"), out var intervalSeconds) && intervalSeconds >= 5)
        {
            options.Interval = TimeSpan.FromSeconds(intervalSeconds);
        }

        if (bool.TryParse(Environment.GetEnvironmentVariable("FENRIR_AGENT_ONCE"), out var once))
        {
            options.Once = once;
        }
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = "";
            return false;
        }

        index++;
        value = args[index];
        return true;
    }
}

public sealed record AgentOptionsLoadResult(bool Success, AgentOptions Options, string? Error)
{
    public static AgentOptionsLoadResult Ok(AgentOptions options) => new(true, options, null);

    public static AgentOptionsLoadResult Fail(string error) => new(false, new AgentOptions(), error);
}

internal sealed class AgentOptionsFile
{
    public string? ApiBaseUrl { get; set; }
    public string? SourceName { get; set; }
    public string? BearerToken { get; set; }
    public int? IntervalSeconds { get; set; }
    public int? BatchSize { get; set; }
    public bool? RegisterSource { get; set; }
    public bool? CaptureProcesses { get; set; }
    public bool? CaptureNetworkConnections { get; set; }
    public bool? EmitProcessInventoryOnFirstRun { get; set; }
    public bool? EmitNetworkInventoryOnFirstRun { get; set; }

    public AgentOptions ToOptions()
    {
        var options = new AgentOptions();
        if (Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out var apiUrl))
        {
            options.ApiBaseUrl = apiUrl;
        }

        if (!string.IsNullOrWhiteSpace(SourceName))
        {
            options.SourceName = SourceName;
        }

        options.BearerToken = string.IsNullOrWhiteSpace(BearerToken) ? null : BearerToken;
        options.Interval = TimeSpan.FromSeconds(Math.Max(IntervalSeconds ?? 30, 5));
        options.BatchSize = Math.Clamp(BatchSize ?? 100, 1, 1000);
        options.RegisterSource = RegisterSource ?? true;
        options.CaptureProcesses = CaptureProcesses ?? true;
        options.CaptureNetworkConnections = CaptureNetworkConnections ?? true;
        options.EmitProcessInventoryOnFirstRun = EmitProcessInventoryOnFirstRun ?? true;
        options.EmitNetworkInventoryOnFirstRun = EmitNetworkInventoryOnFirstRun ?? true;
        return options;
    }
}
