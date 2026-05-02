using System.Data;
using System.Text.Json;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/hunts")]
public sealed class HuntsController(FenrirDbContext dbContext) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("packs")]
    public async Task<ActionResult<IReadOnlyList<HuntPackDto>>> ListPacks(CancellationToken cancellationToken)
    {
        await EnsureDefaultHuntPacksAsync(cancellationToken);
        return Ok(await ReadPacksAsync(cancellationToken));
    }

    [HttpPost("packs")]
    public async Task<ActionResult<HuntPackDto>> CreatePack(HuntPackCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO "HuntPacks" ("Id", "Name", "Description", "Category", "Severity", "MitreTactic", "MitreTechnique", "IsEnabled", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @Name, @Description, @Category, @Severity, @MitreTactic, @MitreTechnique, @IsEnabled, @CreatedAtUtc, @UpdatedAtUtc)
            """, cancellationToken,
            ("Id", id), ("Name", request.Name), ("Description", request.Description), ("Category", request.Category), ("Severity", request.Severity),
            ("MitreTactic", request.MitreTactic), ("MitreTechnique", request.MitreTechnique), ("IsEnabled", request.IsEnabled), ("CreatedAtUtc", now), ("UpdatedAtUtc", now));

        var pack = (await ReadPacksAsync(cancellationToken)).First(item => item.Id == id);
        return Created($"/api/hunts/packs/{id}", pack);
    }

    [HttpPost("packs/{id:guid}/queries")]
    public async Task<ActionResult<HuntPackDto>> AddQuery(Guid id, HuntQueryCreateRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync("""
            INSERT INTO "HuntQueries" ("Id", "HuntPackId", "Name", "Description", "QueryType", "QueryDefinition", "TargetField", "ExpectedEvidence", "SortOrder")
            VALUES (@Id, @HuntPackId, @Name, @Description, @QueryType, @QueryDefinition, @TargetField, @ExpectedEvidence, @SortOrder)
            """, cancellationToken,
            ("Id", Guid.NewGuid()), ("HuntPackId", id), ("Name", request.Name), ("Description", request.Description),
            ("QueryType", request.QueryType), ("QueryDefinition", request.QueryDefinition), ("TargetField", request.TargetField),
            ("ExpectedEvidence", request.ExpectedEvidence), ("SortOrder", request.SortOrder));

        var pack = (await ReadPacksAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);
        return pack is null ? NotFound() : Ok(pack);
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<HuntRunDto>>> ListRuns(CancellationToken cancellationToken)
    {
        return Ok(await ReadRunsAsync(cancellationToken));
    }

    [HttpPost("runs")]
    public async Task<ActionResult<HuntRunDto>> RunPack(HuntRunRequest request, CancellationToken cancellationToken)
    {
        await EnsureDefaultHuntPacksAsync(cancellationToken);
        var pack = (await ReadPacksAsync(cancellationToken)).FirstOrDefault(item => item.Id == request.HuntPackId);
        if (pack is null) return NotFound("Hunt pack not found.");

        var runId = Guid.NewGuid();
        var started = DateTime.UtcNow;
        await ExecuteAsync("""
            INSERT INTO "HuntRuns" ("Id", "HuntPackId", "HuntPackName", "Status", "LookbackHours", "StartedBy", "Scope", "CaseId", "StartedAtUtc", "CompletedAtUtc", "Matches")
            VALUES (@Id, @HuntPackId, @HuntPackName, @Status, @LookbackHours, @StartedBy, @Scope, @CaseId, @StartedAtUtc, @CompletedAtUtc, @Matches)
            """, cancellationToken,
            ("Id", runId), ("HuntPackId", pack.Id), ("HuntPackName", pack.Name), ("Status", "Running"), ("LookbackHours", Math.Clamp(request.LookbackHours, 1, 2160)),
            ("StartedBy", request.StartedBy), ("Scope", request.Scope), ("CaseId", request.CaseId), ("StartedAtUtc", started), ("CompletedAtUtc", null), ("Matches", 0));

        var since = DateTime.UtcNow.AddHours(-Math.Clamp(request.LookbackHours, 1, 2160));
        var events = await dbContext.SiemEvents.AsNoTracking().Where(item => item.TimestampUtc >= since).OrderByDescending(item => item.TimestampUtc).Take(5000).ToListAsync(cancellationToken);
        var matches = 0;
        foreach (var query in pack.Queries.OrderBy(item => item.SortOrder))
        {
            foreach (var match in EvaluateQuery(query, events).Take(100))
            {
                matches++;
                await ExecuteAsync("""
                    INSERT INTO "HuntRunResults" ("Id", "HuntRunId", "HuntQueryId", "QueryName", "EventId", "Severity", "Summary", "Evidence", "CreatedAtUtc")
                    VALUES (@Id, @HuntRunId, @HuntQueryId, @QueryName, @EventId, @Severity, @Summary, @Evidence, @CreatedAtUtc)
                    """, cancellationToken,
                    ("Id", Guid.NewGuid()), ("HuntRunId", runId), ("HuntQueryId", query.Id), ("QueryName", query.Name), ("EventId", match.Id),
                    ("Severity", string.IsNullOrWhiteSpace(match.Severity) ? pack.Severity : match.Severity),
                    ("Summary", $"{query.Name}: {match.EventType} on {match.Host}"),
                    ("Evidence", BuildEvidence(match)), ("CreatedAtUtc", DateTime.UtcNow));
            }
        }

        await ExecuteAsync("UPDATE \"HuntRuns\" SET \"Status\" = @Status, \"CompletedAtUtc\" = @CompletedAtUtc, \"Matches\" = @Matches WHERE \"Id\" = @Id", cancellationToken,
            ("Id", runId), ("Status", matches > 0 ? "CompletedWithMatches" : "Completed"), ("CompletedAtUtc", DateTime.UtcNow), ("Matches", matches));

        var run = (await ReadRunsAsync(cancellationToken)).First(item => item.Id == runId);
        return Created($"/api/hunts/runs/{runId}", run);
    }

    [HttpGet("dfir-collections")]
    public async Task<ActionResult<IReadOnlyList<DfirCollectionDto>>> ListDfirCollections(CancellationToken cancellationToken)
    {
        return Ok(await ReadDfirCollectionsAsync(cancellationToken));
    }

    [HttpPost("dfir-collections")]
    public async Task<ActionResult<DfirCollectionDto>> CreateDfirCollection(DfirCollectionRequest request, CancellationToken cancellationToken)
    {
        var artefacts = request.Artefacts is { Count: > 0 }
            ? request.Artefacts
            : request.CollectionType.Equals("deep", StringComparison.OrdinalIgnoreCase)
                ? ["processes", "network_connections", "services", "scheduled_tasks", "autoruns", "event_logs", "powershell_history", "dns_cache", "users_groups", "file_hashes"]
                : ["processes", "network_connections", "services", "scheduled_tasks", "event_logs"];

        var id = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO "DfirCollections" ("Id", "Hostname", "CollectionType", "Status", "CaseId", "RequestedBy", "ArtefactsJson", "Notes", "RequestedAtUtc", "CompletedAtUtc", "EvidenceBundlePath", "ErrorSummary")
            VALUES (@Id, @Hostname, @CollectionType, @Status, @CaseId, @RequestedBy, @ArtefactsJson, @Notes, @RequestedAtUtc, @CompletedAtUtc, @EvidenceBundlePath, @ErrorSummary)
            """, cancellationToken,
            ("Id", id), ("Hostname", request.Hostname), ("CollectionType", request.CollectionType), ("Status", "Queued"), ("CaseId", request.CaseId),
            ("RequestedBy", request.RequestedBy), ("ArtefactsJson", JsonSerializer.Serialize(artefacts, JsonOptions)), ("Notes", request.Notes),
            ("RequestedAtUtc", DateTime.UtcNow), ("CompletedAtUtc", null), ("EvidenceBundlePath", null), ("ErrorSummary", null));

        var collection = (await ReadDfirCollectionsAsync(cancellationToken)).First(item => item.Id == id);
        return Created($"/api/hunts/dfir-collections/{id}", collection);
    }

    private IEnumerable<dynamic> EvaluateQuery(HuntQueryDto query, IEnumerable<dynamic> events)
    {
        var definition = query.QueryDefinition ?? string.Empty;
        var tokens = definition.Split([' ', ',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length > 2).ToArray();
        if (tokens.Length == 0) tokens = [query.Name];

        return events.Where(item => tokens.Any(token => Contains(GetFieldValue(item, query.TargetField), token)
            || Contains(item.Message, token)
            || Contains(item.RawJson, token)
            || Contains(item.EventType, token)
            || Contains(item.CommandLine, token)
            || Contains(item.Action, token)));
    }

    private static string? GetFieldValue(dynamic item, string field) => field.ToLowerInvariant() switch
    {
        "user" => item.User,
        "host" => item.Host,
        "sourceip" => item.SourceIp,
        "destinationip" => item.DestinationIp,
        "domain" => item.Domain,
        "filehashsha256" or "hash" => item.FileHashSha256,
        "processname" => item.ProcessName,
        "commandline" => item.CommandLine,
        "action" => item.Action,
        "eventcategory" => item.EventCategory,
        "eventtype" => item.EventType,
        _ => item.Message
    };

    private static string BuildEvidence(dynamic item)
    {
        return $"Time={item.TimestampUtc:u}; Severity={item.Severity}; User={item.User}; Host={item.Host}; SourceIp={item.SourceIp}; DestinationIp={item.DestinationIp}; Domain={item.Domain}; Action={item.Action}; Message={item.Message}";
    }

    private async Task EnsureDefaultHuntPacksAsync(CancellationToken cancellationToken)
    {
        var count = Convert.ToInt32(await ScalarAsync("SELECT COUNT(*) FROM \"HuntPacks\"", cancellationToken));
        if (count > 0) return;

        var defaults = new[]
        {
            new HuntPackCreateRequest("Suspicious PowerShell hunt", "Find encoded, download or web-enabled PowerShell activity.", "endpoint", "High", "Execution", "T1059.001"),
            new HuntPackCreateRequest("Credential access hunt", "Find repeated authentication failures, unusual success and account abuse indicators.", "identity", "High", "Credential Access", "T1110"),
            new HuntPackCreateRequest("Mailbox abuse hunt", "Find mailbox forwarding, inbox rule and OAuth-style suspicious activity.", "email", "High", "Collection", "T1114"),
            new HuntPackCreateRequest("Cloud control-plane hunt", "Find role, policy, access key and administrative cloud changes.", "cloud", "High", "Privilege Escalation", "T1098")
        };

        foreach (var create in defaults)
        {
            var result = await CreatePack(create, cancellationToken);
            var pack = ((CreatedResult)result.Result!).Value as HuntPackDto;
            if (pack is null) continue;
            var queries = create.Category switch
            {
                "endpoint" => new[]
                {
                    new HuntQueryCreateRequest("Encoded PowerShell", "PowerShell command line contains encoded execution patterns.", "siem_structured", "encodedcommand -enc frombase64string", "CommandLine", "Suspicious encoded command line", 10),
                    new HuntQueryCreateRequest("PowerShell download cradle", "PowerShell references web download execution patterns.", "siem_structured", "downloadstring invoke-webrequest iwr http https", "CommandLine", "Download or web execution command", 20)
                },
                "identity" => new[]
                {
                    new HuntQueryCreateRequest("Failed login cluster", "Authentication failures that may indicate password spraying or brute force.", "siem_structured", "failed failure invalid password login", "EventCategory", "Authentication failure evidence", 10),
                    new HuntQueryCreateRequest("Successful login after failures", "Successful authentication activity requiring correlation review.", "siem_structured", "success signin login", "Outcome", "Successful authentication evidence", 20)
                },
                "email" => new[]
                {
                    new HuntQueryCreateRequest("Mailbox forwarding", "Mailbox forwarding or redirect rules detected.", "siem_structured", "forward redirect inbox rule mailbox", "Action", "Mailbox rule or forwarding evidence", 10),
                    new HuntQueryCreateRequest("OAuth grant activity", "OAuth or application consent activity in mailbox/cloud logs.", "siem_structured", "oauth consent app grant", "Action", "OAuth consent evidence", 20)
                },
                _ => new[]
                {
                    new HuntQueryCreateRequest("Cloud admin role changes", "Cloud role or privilege assignment activity.", "siem_structured", "role admin policy privilege assignment", "Action", "Privilege change evidence", 10),
                    new HuntQueryCreateRequest("Cloud key creation", "Access key or token creation activity.", "siem_structured", "accesskey create token secret credential", "Action", "Credential creation evidence", 20)
                }
            };
            foreach (var query in queries) await AddQuery(pack.Id, query, cancellationToken);
        }
    }

    private async Task<List<HuntPackDto>> ReadPacksAsync(CancellationToken cancellationToken)
    {
        var packs = new List<HuntPackDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"HuntPacks\" ORDER BY \"CreatedAtUtc\"", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = GetGuid(reader, "Id");
            packs.Add(new HuntPackDto(id, GetString(reader, "Name"), GetString(reader, "Description"), GetString(reader, "Category"), GetString(reader, "Severity"), GetString(reader, "MitreTactic"), GetNullableString(reader, "MitreTechnique"), GetBool(reader, "IsEnabled"), GetDate(reader, "CreatedAtUtc"), GetDate(reader, "UpdatedAtUtc"), await ReadQueriesAsync(id, cancellationToken)));
        }
        return packs;
    }

    private async Task<List<HuntQueryDto>> ReadQueriesAsync(Guid packId, CancellationToken cancellationToken)
    {
        var queries = new List<HuntQueryDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"HuntQueries\" WHERE \"HuntPackId\" = @HuntPackId ORDER BY \"SortOrder\"", cancellationToken, ("HuntPackId", packId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            queries.Add(new HuntQueryDto(GetGuid(reader, "Id"), GetGuid(reader, "HuntPackId"), GetString(reader, "Name"), GetString(reader, "Description"), GetString(reader, "QueryType"), GetString(reader, "QueryDefinition"), GetString(reader, "TargetField"), GetNullableString(reader, "ExpectedEvidence"), GetInt(reader, "SortOrder")));
        }
        return queries;
    }

    private async Task<List<HuntRunDto>> ReadRunsAsync(CancellationToken cancellationToken)
    {
        var runs = new List<HuntRunDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"HuntRuns\" ORDER BY \"StartedAtUtc\" DESC LIMIT 100", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = GetGuid(reader, "Id");
            runs.Add(new HuntRunDto(id, GetGuid(reader, "HuntPackId"), GetString(reader, "HuntPackName"), GetString(reader, "Status"), GetInt(reader, "LookbackHours"), GetString(reader, "StartedBy"), GetNullableString(reader, "Scope"), GetNullableGuid(reader, "CaseId"), GetDate(reader, "StartedAtUtc"), GetNullableDate(reader, "CompletedAtUtc"), GetInt(reader, "Matches"), await ReadResultsAsync(id, cancellationToken)));
        }
        return runs;
    }

    private async Task<List<HuntRunResultDto>> ReadResultsAsync(Guid runId, CancellationToken cancellationToken)
    {
        var results = new List<HuntRunResultDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"HuntRunResults\" WHERE \"HuntRunId\" = @HuntRunId ORDER BY \"CreatedAtUtc\" DESC", cancellationToken, ("HuntRunId", runId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new HuntRunResultDto(GetGuid(reader, "Id"), GetGuid(reader, "HuntRunId"), GetGuid(reader, "HuntQueryId"), GetString(reader, "QueryName"), GetNullableGuid(reader, "EventId"), GetString(reader, "Severity"), GetString(reader, "Summary"), GetString(reader, "Evidence"), GetDate(reader, "CreatedAtUtc")));
        }
        return results;
    }

    private async Task<List<DfirCollectionDto>> ReadDfirCollectionsAsync(CancellationToken cancellationToken)
    {
        var items = new List<DfirCollectionDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"DfirCollections\" ORDER BY \"RequestedAtUtc\" DESC LIMIT 100", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var artefacts = JsonSerializer.Deserialize<List<string>>(GetString(reader, "ArtefactsJson"), JsonOptions) ?? [];
            items.Add(new DfirCollectionDto(GetGuid(reader, "Id"), GetString(reader, "Hostname"), GetString(reader, "CollectionType"), GetString(reader, "Status"), GetNullableGuid(reader, "CaseId"), GetString(reader, "RequestedBy"), artefacts, GetNullableString(reader, "Notes"), GetDate(reader, "RequestedAtUtc"), GetNullableDate(reader, "CompletedAtUtc"), GetNullableString(reader, "EvidenceBundlePath"), GetNullableString(reader, "ErrorSummary")));
        }
        return items;
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<object?> ScalarAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    private async Task<System.Data.Common.DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            var dbParameter = command.CreateParameter();
            dbParameter.ParameterName = $"@{parameter.Name}";
            dbParameter.Value = parameter.Value ?? DBNull.Value;
            command.Parameters.Add(dbParameter);
        }
        return command;
    }

    private static bool Contains(string? source, string value) => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;
    private static Guid GetGuid(IDataRecord record, string name) => record.GetGuid(record.GetOrdinal(name));
    private static Guid? GetNullableGuid(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetGuid(record.GetOrdinal(name));
    private static string GetString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? string.Empty : record.GetString(record.GetOrdinal(name));
    private static string? GetNullableString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetString(record.GetOrdinal(name));
    private static bool GetBool(IDataRecord record, string name) => record.GetBoolean(record.GetOrdinal(name));
    private static int GetInt(IDataRecord record, string name) => record.GetInt32(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
    private static DateTime? GetNullableDate(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetDateTime(record.GetOrdinal(name));
}
