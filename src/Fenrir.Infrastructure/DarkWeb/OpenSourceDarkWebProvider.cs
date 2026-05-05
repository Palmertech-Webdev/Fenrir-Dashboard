using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Microsoft.Extensions.Configuration;

namespace Fenrir.Infrastructure.DarkWeb;

public sealed class DarkWebProviderOptions
{
    public bool EnableLocalDataset { get; init; } = true;
    public string LocalDatasetPath { get; init; } = "data/darkweb/exposures.csv";
    public bool EnableXposedOrNot { get; init; } = true;
    public string XposedOrNotBaseUrl { get; init; } = "https://api.xposedornot.com";
    public bool EnableLeakCheck { get; init; } = true;
    public string LeakCheckBaseUrl { get; init; } = "https://leakcheck.io";
    public int TimeoutSeconds { get; init; } = 10;

    public static DarkWebProviderOptions FromConfiguration(IConfiguration configuration)
    {
        return new DarkWebProviderOptions
        {
            EnableLocalDataset = GetBoolean(configuration, "EnableLocalDataset", true),
            LocalDatasetPath = GetString(configuration, "LocalDatasetPath", "data/darkweb/exposures.csv"),
            EnableXposedOrNot = GetBoolean(configuration, "EnableXposedOrNot", true),
            XposedOrNotBaseUrl = GetString(configuration, "XposedOrNotBaseUrl", "https://api.xposedornot.com"),
            EnableLeakCheck = GetBoolean(configuration, "EnableLeakCheck", true),
            LeakCheckBaseUrl = GetString(configuration, "LeakCheckBaseUrl", "https://leakcheck.io"),
            TimeoutSeconds = GetInt(configuration, "TimeoutSeconds", 10)
        };
    }

    private static bool GetBoolean(IConfiguration configuration, string key, bool fallback) =>
        bool.TryParse(configuration[key], out var value) ? value : fallback;

    private static int GetInt(IConfiguration configuration, string key, int fallback) =>
        int.TryParse(configuration[key], out var value) ? value : fallback;

    private static string GetString(IConfiguration configuration, string key, string fallback) =>
        string.IsNullOrWhiteSpace(configuration[key]) ? fallback : configuration[key]!;
}

public sealed class OpenSourceDarkWebProvider : IDarkWebProvider, IDisposable
{
    private readonly DarkWebProviderOptions options;
    private readonly LocalDarkWebExposureStore localStore;
    private readonly XposedOrNotDarkWebProvider xposedOrNotProvider;
    private readonly LeakCheckDarkWebProvider leakCheckProvider;

    public OpenSourceDarkWebProvider(DarkWebProviderOptions options)
    {
        this.options = options;
        localStore = new LocalDarkWebExposureStore(options.LocalDatasetPath);
        xposedOrNotProvider = new XposedOrNotDarkWebProvider(options);
        leakCheckProvider = new LeakCheckDarkWebProvider(options);
    }

    public async Task<DarkWebProviderResult> CheckEmailAsync(string email, CancellationToken cancellationToken)
    {
        var results = new List<DarkWebProviderResult>();
        if (options.EnableLocalDataset)
        {
            results.Add(await localStore.CheckEmailAsync(email, cancellationToken));
        }

        if (options.EnableXposedOrNot)
        {
            results.Add(await xposedOrNotProvider.CheckEmailAsync(email, cancellationToken));
        }

        if (options.EnableLeakCheck)
        {
            results.Add(await leakCheckProvider.CheckEmailAsync(email, cancellationToken));
        }

        return Merge(results);
    }

    public async Task<DarkWebProviderResult> CheckDomainAsync(string domain, CancellationToken cancellationToken)
    {
        var results = new List<DarkWebProviderResult>();
        if (options.EnableLocalDataset)
        {
            results.Add(await localStore.CheckDomainAsync(domain, cancellationToken));
        }

        if (options.EnableXposedOrNot)
        {
            results.Add(await xposedOrNotProvider.CheckDomainAsync(domain, cancellationToken));
        }

        if (options.EnableLeakCheck)
        {
            results.Add(await leakCheckProvider.CheckDomainAsync(domain, cancellationToken));
        }

        return Merge(results);
    }

    public async Task<DarkWebProviderResult> CheckUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var results = new List<DarkWebProviderResult>();
        if (options.EnableLocalDataset)
        {
            results.Add(await localStore.CheckUsernameAsync(username, cancellationToken));
        }

        if (options.EnableXposedOrNot)
        {
            results.Add(await xposedOrNotProvider.CheckUsernameAsync(username, cancellationToken));
        }

        if (options.EnableLeakCheck)
        {
            results.Add(await leakCheckProvider.CheckUsernameAsync(username, cancellationToken));
        }

        return Merge(results);
    }

    public void Dispose()
    {
        xposedOrNotProvider.Dispose();
        leakCheckProvider.Dispose();
    }

    private static DarkWebProviderResult Merge(IReadOnlyList<DarkWebProviderResult> results)
    {
        if (results.Count == 0)
        {
            return new DarkWebProviderResult(false, 0, ["No exposure providers enabled"]);
        }

        if (results.Any(result => result.Exposed))
        {
            var exposedSources = results
                .Where(result => result.Exposed)
                .SelectMany(result => result.Sources)
                .Where(source => !string.IsNullOrWhiteSpace(source))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var breachCount = Math.Max(
                exposedSources.Length,
                results.Where(result => result.Exposed).Sum(result => Math.Max(1, result.BreachCount)));

            return new DarkWebProviderResult(true, breachCount, exposedSources);
        }

        var sources = results
            .SelectMany(result => result.Sources)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DarkWebProviderResult(false, 0, sources.Length == 0 ? ["No public exposure matches in enabled providers"] : sources);
    }
}

public sealed class LocalDarkWebExposureStore
{
    private readonly string configuredPath;
    private readonly object cacheLock = new();
    private string? resolvedPath;
    private DateTime lastWriteUtc;
    private IReadOnlyList<LocalExposureRecord>? cachedRecords;

    public LocalDarkWebExposureStore(string configuredPath)
    {
        this.configuredPath = configuredPath;
    }

    public Task<DarkWebProviderResult> CheckEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(email);
        var records = ReadRecords(cancellationToken)
            .Where(record => record.QueryType.Equals("Email", StringComparison.OrdinalIgnoreCase)
                && record.Query.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(ToResult(records, "Local exposure dataset: no email match"));
    }

    public Task<DarkWebProviderResult> CheckDomainAsync(string domain, CancellationToken cancellationToken)
    {
        var normalizedDomain = NormalizeDomain(domain);
        var records = ReadRecords(cancellationToken)
            .Where(record =>
                record.QueryType.Equals("Domain", StringComparison.OrdinalIgnoreCase)
                    && DomainMatches(record.Query, normalizedDomain)
                || record.QueryType.Equals("Email", StringComparison.OrdinalIgnoreCase)
                    && DomainMatches(GetEmailDomain(record.Query), normalizedDomain))
            .ToArray();

        return Task.FromResult(ToResult(records, "Local exposure dataset: no domain match"));
    }

    public Task<DarkWebProviderResult> CheckUsernameAsync(string username, CancellationToken cancellationToken)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var records = ReadRecords(cancellationToken)
            .Where(record => record.QueryType.Equals("Username", StringComparison.OrdinalIgnoreCase)
                && record.Query.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return Task.FromResult(ToResult(records, "Local exposure dataset: no username match"));
    }

    private IReadOnlyList<LocalExposureRecord> ReadRecords(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath();
        if (path is null)
        {
            return [];
        }

        var lastWrite = File.GetLastWriteTimeUtc(path);
        lock (cacheLock)
        {
            if (cachedRecords is not null
                && string.Equals(path, resolvedPath, StringComparison.OrdinalIgnoreCase)
                && lastWrite == lastWriteUtc)
            {
                return cachedRecords;
            }

            var records = File.ReadLines(path)
                .Select((line, index) => ParseLine(line, index))
                .Where(record => record is not null)
                .Cast<LocalExposureRecord>()
                .ToArray();

            resolvedPath = path;
            lastWriteUtc = lastWrite;
            cachedRecords = records;
            return records;
        }
    }

    private string? ResolvePath()
    {
        if (Path.IsPathFullyQualified(configuredPath))
        {
            return File.Exists(configuredPath) ? configuredPath : null;
        }

        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, configuredPath),
            Path.Combine(AppContext.BaseDirectory, configuredPath)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static LocalExposureRecord? ParseLine(string line, int index)
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
        {
            return null;
        }

        var columns = SplitCsvLine(line);
        if (columns.Length < 3)
        {
            return null;
        }

        if (index == 0 && columns[0].Equals("query", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var queryType = columns[1].Trim();
        var normalizedQuery = queryType.ToLowerInvariant() switch
        {
            "email" => NormalizeEmail(columns[0]),
            "domain" => NormalizeDomain(columns[0]),
            "username" => columns[0].Trim().ToLowerInvariant(),
            _ => columns[0].Trim().ToLowerInvariant()
        };

        if (string.IsNullOrWhiteSpace(normalizedQuery) || string.IsNullOrWhiteSpace(queryType) || string.IsNullOrWhiteSpace(columns[2]))
        {
            return null;
        }

        _ = int.TryParse(columns.ElementAtOrDefault(4), out var exposureCount);
        return new LocalExposureRecord(
            normalizedQuery,
            queryType,
            columns[2].Trim(),
            columns.ElementAtOrDefault(3)?.Trim() ?? string.Empty,
            Math.Max(1, exposureCount),
            columns.ElementAtOrDefault(5)?.Trim() ?? string.Empty);
    }

    private static string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        var current = new List<char>();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];
            if (character == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Add('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (character == ',' && !inQuotes)
            {
                values.Add(new string(current.ToArray()));
                current.Clear();
                continue;
            }

            current.Add(character);
        }

        values.Add(new string(current.ToArray()));
        return values.ToArray();
    }

    private static DarkWebProviderResult ToResult(IReadOnlyList<LocalExposureRecord> records, string noMatchMessage)
    {
        if (records.Count == 0)
        {
            return new DarkWebProviderResult(false, 0, [noMatchMessage]);
        }

        var sources = records
            .Select(record => record.ToSourceLabel())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DarkWebProviderResult(true, sources.Length, sources);
    }

    private static string NormalizeEmail(string email) => email.Trim().Trim('<', '>').ToLowerInvariant();

    private static string NormalizeDomain(string domain)
    {
        var value = domain.Trim().TrimEnd('.').ToLowerInvariant();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
        {
            value = uri.Host;
        }

        var slashIndex = value.IndexOf('/');
        if (slashIndex >= 0)
        {
            value = value[..slashIndex];
        }

        return value.Trim().TrimEnd('.');
    }

    private static string GetEmailDomain(string email)
    {
        var atIndex = email.LastIndexOf('@');
        return atIndex < 0 ? string.Empty : NormalizeDomain(email[(atIndex + 1)..]);
    }

    private static bool DomainMatches(string candidate, string domain)
    {
        var normalizedCandidate = NormalizeDomain(candidate);
        return normalizedCandidate.Equals(domain, StringComparison.OrdinalIgnoreCase)
            || normalizedCandidate.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class LocalDarkWebExposureImportService : IDarkWebExposureImportService
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    private readonly DarkWebProviderOptions options;

    public LocalDarkWebExposureImportService(DarkWebProviderOptions options)
    {
        this.options = options;
    }

    public async Task<DarkWebExposureImportResponse> ImportAsync(DarkWebExposureImportRequest request, CancellationToken cancellationToken)
    {
        var skipped = new List<string>();
        var lines = new List<string>();

        foreach (var item in request.Items ?? [])
        {
            var normalized = NormalizeImportItem(item, skipped);
            if (normalized is null)
            {
                continue;
            }

            lines.Add(ToCsvLine(normalized));
        }

        if (lines.Count == 0)
        {
            return new DarkWebExposureImportResponse(0, skipped);
        }

        var path = ResolveWritePath(options.LocalDatasetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await FileLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(path))
            {
                await File.WriteAllTextAsync(path, "query,queryType,sourceName,breachDate,exposureCount,description" + Environment.NewLine, cancellationToken);
            }

            await File.AppendAllLinesAsync(path, lines, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }

        return new DarkWebExposureImportResponse(lines.Count, skipped);
    }

    private static DarkWebExposureImportItem? NormalizeImportItem(DarkWebExposureImportItem item, ICollection<string> skipped)
    {
        var query = item.Query?.Trim() ?? string.Empty;
        var queryType = item.QueryType?.Trim() ?? string.Empty;
        var sourceName = item.SourceName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(queryType) || string.IsNullOrWhiteSpace(sourceName))
        {
            skipped.Add("Skipped row with missing query, query type, or source name.");
            return null;
        }

        if (!queryType.Equals("Email", StringComparison.OrdinalIgnoreCase)
            && !queryType.Equals("Domain", StringComparison.OrdinalIgnoreCase)
            && !queryType.Equals("Username", StringComparison.OrdinalIgnoreCase))
        {
            skipped.Add($"Skipped {query}: query type must be Email, Domain, or Username.");
            return null;
        }

        var normalizedType = char.ToUpperInvariant(queryType[0]) + queryType[1..].ToLowerInvariant();
        var normalizedQuery = normalizedType switch
        {
            "Email" => query.Trim('<', '>').ToLowerInvariant(),
            "Domain" => query.Trim().TrimEnd('.').ToLowerInvariant(),
            "Username" => query.ToLowerInvariant(),
            _ => query
        };

        if (normalizedType == "Email" && !normalizedQuery.Contains('@'))
        {
            skipped.Add($"Skipped {query}: email rows must contain @.");
            return null;
        }

        return new DarkWebExposureImportItem(
            normalizedQuery,
            normalizedType,
            sourceName,
            Truncate(item.BreachDate?.Trim() ?? string.Empty, 40),
            Math.Max(1, item.ExposureCount),
            Truncate(item.Description?.Trim() ?? string.Empty, 500));
    }

    private static string ResolveWritePath(string configuredPath)
    {
        if (Path.IsPathFullyQualified(configuredPath))
        {
            return configuredPath;
        }

        return Path.Combine(Environment.CurrentDirectory, configuredPath);
    }

    private static string ToCsvLine(DarkWebExposureImportItem item) =>
        string.Join(',', new[]
        {
            CsvEscape(item.Query),
            CsvEscape(item.QueryType),
            CsvEscape(item.SourceName),
            CsvEscape(item.BreachDate ?? string.Empty),
            item.ExposureCount.ToString(),
            CsvEscape(item.Description ?? string.Empty)
        });

    private static string CsvEscape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}

public sealed class LeakCheckDarkWebProvider : IDarkWebProvider, IDisposable
{
    private readonly HttpClient httpClient;

    public LeakCheckDarkWebProvider(DarkWebProviderOptions options)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(NormalizeBaseUrl(options.LeakCheckBaseUrl)),
            Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 2, 30))
        };

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Fenrir-SOC-Core/1.0");
    }

    public Task<DarkWebProviderResult> CheckEmailAsync(string email, CancellationToken cancellationToken) =>
        QueryAsync(email, "LeakCheck: no public email breach match", cancellationToken);

    public Task<DarkWebProviderResult> CheckDomainAsync(string domain, CancellationToken cancellationToken) =>
        Task.FromResult(new DarkWebProviderResult(false, 0, ["LeakCheck public API supports email/username checks, not domain-wide checks"]));

    public Task<DarkWebProviderResult> CheckUsernameAsync(string username, CancellationToken cancellationToken) =>
        QueryAsync(username, "LeakCheck: no public username breach match", cancellationToken);

    public void Dispose() => httpClient.Dispose();

    private async Task<DarkWebProviderResult> QueryAsync(string query, string noMatchMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync($"api/public?check={Uri.EscapeDataString(query.Trim())}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new DarkWebProviderResult(false, 0, [noMatchMessage]);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new DarkWebProviderResult(false, 0, ["LeakCheck rate limit reached; retry later"]);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new DarkWebProviderResult(false, 0, [$"LeakCheck unavailable: HTTP {(int)response.StatusCode}"]);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ToProviderResult(document.RootElement, noMatchMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DarkWebProviderResult(false, 0, ["LeakCheck request timed out"]);
        }
        catch (HttpRequestException exception)
        {
            return new DarkWebProviderResult(false, 0, [$"LeakCheck request failed: {exception.Message}"]);
        }
        catch (JsonException)
        {
            return new DarkWebProviderResult(false, 0, ["LeakCheck returned an unreadable JSON response"]);
        }
    }

    private static DarkWebProviderResult ToProviderResult(JsonElement root, string noMatchMessage)
    {
        var success = root.TryGetProperty("success", out var successElement)
            && successElement.ValueKind == JsonValueKind.True;
        if (!success)
        {
            return new DarkWebProviderResult(false, 0, [noMatchMessage]);
        }

        var found = root.TryGetProperty("found", out var foundElement) && foundElement.TryGetInt32(out var foundCount)
            ? foundCount
            : 1;
        var sources = new List<string>();

        if (root.TryGetProperty("sources", out var sourcesElement) && sourcesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var source in sourcesElement.EnumerateArray())
            {
                if (source.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = source.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;
                var date = source.TryGetProperty("date", out var dateElement) ? dateElement.GetString() : null;
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var label = string.IsNullOrWhiteSpace(date)
                    ? $"LeakCheck: {name}"
                    : $"LeakCheck: {name} ({date})";
                sources.Add(label);
            }
        }

        if (sources.Count == 0)
        {
            sources.Add("LeakCheck public breach match");
        }

        if (found > sources.Count)
        {
            sources[0] = $"{sources[0]} [{found} records]";
        }

        return new DarkWebProviderResult(true, Math.Max(found, sources.Count), sources.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var value = string.IsNullOrWhiteSpace(baseUrl) ? "https://leakcheck.io" : baseUrl.Trim();
        return value.EndsWith('/') ? value : $"{value}/";
    }
}

public sealed class XposedOrNotDarkWebProvider : IDarkWebProvider, IDisposable
{
    private readonly HttpClient httpClient;

    public XposedOrNotDarkWebProvider(DarkWebProviderOptions options)
    {
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(NormalizeBaseUrl(options.XposedOrNotBaseUrl)),
            Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 2, 30))
        };

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Fenrir-SOC-Core/1.0");
    }

    public Task<DarkWebProviderResult> CheckEmailAsync(string email, CancellationToken cancellationToken) =>
        QueryAsync($"v1/check-email/{Uri.EscapeDataString(email.Trim())}", "XposedOrNot: no public email breach match", cancellationToken);

    public Task<DarkWebProviderResult> CheckDomainAsync(string domain, CancellationToken cancellationToken) =>
        QueryAsync($"v1/breaches?domain={Uri.EscapeDataString(domain.Trim())}", "XposedOrNot: no public domain breach match", cancellationToken);

    public Task<DarkWebProviderResult> CheckUsernameAsync(string username, CancellationToken cancellationToken) =>
        Task.FromResult(new DarkWebProviderResult(false, 0, ["XposedOrNot public API does not provide username exposure checks"]));

    public void Dispose() => httpClient.Dispose();

    private async Task<DarkWebProviderResult> QueryAsync(string path, string noMatchMessage, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(path, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new DarkWebProviderResult(false, 0, [noMatchMessage]);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return new DarkWebProviderResult(false, 0, ["XposedOrNot rate limit reached; retry after one second"]);
            }

            if (!response.IsSuccessStatusCode)
            {
                return new DarkWebProviderResult(false, 0, [$"XposedOrNot unavailable: HTTP {(int)response.StatusCode}"]);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ToProviderResult(document.RootElement, noMatchMessage);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DarkWebProviderResult(false, 0, ["XposedOrNot request timed out"]);
        }
        catch (HttpRequestException exception)
        {
            return new DarkWebProviderResult(false, 0, [$"XposedOrNot request failed: {exception.Message}"]);
        }
        catch (JsonException)
        {
            return new DarkWebProviderResult(false, 0, ["XposedOrNot returned an unreadable JSON response"]);
        }
    }

    private static DarkWebProviderResult ToProviderResult(JsonElement root, string noMatchMessage)
    {
        if (ContainsNotFoundError(root))
        {
            return new DarkWebProviderResult(false, 0, [noMatchMessage]);
        }

        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ExtractExposureNames(root, sources);

        var status = FindStringProperty(root, "status");
        var exposed = sources.Count > 0 || string.Equals(status, "success", StringComparison.OrdinalIgnoreCase);
        if (!exposed)
        {
            return new DarkWebProviderResult(false, 0, [noMatchMessage]);
        }

        if (sources.Count == 0)
        {
            sources.Add("XposedOrNot public exposure match");
        }

        return new DarkWebProviderResult(true, sources.Count, sources.ToArray());
    }

    private static void ExtractExposureNames(JsonElement element, ISet<string> sources, bool inExposureContainer = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyName = property.Name.ToLowerInvariant();
                    var nextInExposureContainer = inExposureContainer || IsExposureContainer(propertyName);

                    if (nextInExposureContainer && IsNameProperty(propertyName))
                    {
                        AddStringValue(property.Value, sources);
                    }
                    else if (nextInExposureContainer && property.Value.ValueKind == JsonValueKind.Number && LooksLikeBreachName(property.Name))
                    {
                        sources.Add(property.Name.Trim());
                    }

                    ExtractExposureNames(property.Value, sources, nextInExposureContainer);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (inExposureContainer && item.ValueKind == JsonValueKind.String)
                    {
                        AddSourceName(item.GetString(), sources);
                        continue;
                    }

                    ExtractExposureNames(item, sources, inExposureContainer);
                }
                break;
        }
    }

    private static bool ContainsNotFoundError(JsonElement root)
    {
        var error = FindStringProperty(root, "Error") ?? FindStringProperty(root, "error") ?? FindStringProperty(root, "message");
        return error is not null
            && (error.Contains("not found", StringComparison.OrdinalIgnoreCase)
                || error.Contains("no data", StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindStringProperty(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private static void AddStringValue(JsonElement value, ISet<string> sources)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                AddSourceName(value.GetString(), sources);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    AddStringValue(item, sources);
                }
                break;
            case JsonValueKind.Object:
                ExtractExposureNames(value, sources, inExposureContainer: true);
                break;
        }
    }

    private static bool IsExposureContainer(string propertyName) =>
        propertyName is "breaches"
            or "exposedbreaches"
            or "breaches_details"
            or "breachessummary"
            or "breach_summary"
            or "top10_breaches"
            or "detailed_breach_info"
            || propertyName.Contains("breach", StringComparison.OrdinalIgnoreCase);

    private static bool IsNameProperty(string propertyName) =>
        propertyName is "breach"
            or "breachid"
            or "name"
            or "site"
            or "source"
            or "title";

    private static void AddSourceName(string? value, ISet<string> sources)
    {
        if (value is null)
        {
            return;
        }

        var source = value.Trim();
        if (LooksLikeBreachName(source))
        {
            sources.Add(source);
        }
    }

    private static bool LooksLikeBreachName(string value)
    {
        if (value.Length is < 2 or > 120)
        {
            return false;
        }

        if (value.Equals("success", StringComparison.OrdinalIgnoreCase)
            || value.Equals("error", StringComparison.OrdinalIgnoreCase)
            || value.Equals("not found", StringComparison.OrdinalIgnoreCase)
            || value.Equals("verified", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !DateTimeOffset.TryParse(value, out _) && !value.Contains('{') && !value.Contains('}');
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var value = string.IsNullOrWhiteSpace(baseUrl) ? "https://api.xposedornot.com" : baseUrl.Trim();
        return value.EndsWith('/') ? value : $"{value}/";
    }
}

public sealed record LocalExposureRecord(
    string Query,
    string QueryType,
    string SourceName,
    string BreachDate,
    int ExposureCount,
    string Description)
{
    public string ToSourceLabel()
    {
        var date = string.IsNullOrWhiteSpace(BreachDate) ? string.Empty : $" ({BreachDate})";
        var count = ExposureCount > 1 ? $" [{ExposureCount} exposure summaries]" : string.Empty;
        return $"{SourceName}{date}{count}";
    }
}
