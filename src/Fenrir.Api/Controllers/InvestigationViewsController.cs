using System.Data;
using System.Data.Common;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/investigations/views")]
public sealed class InvestigationViewsController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet("email")]
    public Task<ActionResult<InvestigationViewDto>> Email(
        [FromQuery] string? user,
        [FromQuery] string? mailbox,
        [FromQuery] string? sender,
        [FromQuery] string? recipient,
        [FromQuery] string? domain,
        [FromQuery] string? messageId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var filters = new InvestigationFilters(
            ViewType: "email",
            User: user,
            Mailbox: mailbox,
            Domain: domain,
            SourceIp: null,
            DestinationIp: null,
            Host: null,
            ProcessName: null,
            CloudTenantId: null,
            CloudResourceId: null,
            Action: null,
            ExtraQuery: sender ?? recipient ?? messageId,
            Categories: ["email", "mailbox", "m365", "authentication"],
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Take: take <= 0 ? 250 : take);

        return BuildViewAsync(filters, "Email investigation", "Mailbox, sender, recipient, URL, attachment and sign-in pivots", EmailQuestions, EmailActions, cancellationToken);
    }

    [HttpGet("cloud")]
    public Task<ActionResult<InvestigationViewDto>> Cloud(
        [FromQuery] string? user,
        [FromQuery] string? ipAddress,
        [FromQuery] string? tenantId,
        [FromQuery] string? resourceId,
        [FromQuery] string? action,
        [FromQuery] string? region,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var filters = new InvestigationFilters(
            ViewType: "cloud",
            User: user,
            Mailbox: null,
            Domain: null,
            SourceIp: ipAddress,
            DestinationIp: null,
            Host: null,
            ProcessName: null,
            CloudTenantId: tenantId,
            CloudResourceId: resourceId,
            Action: action,
            ExtraQuery: region,
            Categories: ["cloud", "aws", "m365", "authentication", "iam"],
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Take: take <= 0 ? 250 : take);

        return BuildViewAsync(filters, "Cloud investigation", "User, IP, tenant, resource, API action and control-plane pivots", CloudQuestions, CloudActions, cancellationToken);
    }

    [HttpGet("windows")]
    public Task<ActionResult<InvestigationViewDto>> Windows(
        [FromQuery] string? host,
        [FromQuery] string? user,
        [FromQuery] string? process,
        [FromQuery] string? ipAddress,
        [FromQuery] string? hash,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int take,
        CancellationToken cancellationToken)
    {
        var filters = new InvestigationFilters(
            ViewType: "windows",
            User: user,
            Mailbox: null,
            Domain: null,
            SourceIp: ipAddress,
            DestinationIp: null,
            Host: host,
            ProcessName: process,
            CloudTenantId: null,
            CloudResourceId: null,
            Action: null,
            ExtraQuery: hash,
            Categories: ["windows", "endpoint", "process", "powershell", "authentication", "network"],
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Take: take <= 0 ? 250 : take);

        return BuildViewAsync(filters, "Windows investigation", "Host, user, process, command line, PowerShell, network and file-hash pivots", WindowsQuestions, WindowsActions, cancellationToken);
    }

    private async Task<ActionResult<InvestigationViewDto>> BuildViewAsync(
        InvestigationFilters filters,
        string title,
        string scopeDescription,
        IReadOnlyList<string> analystQuestions,
        IReadOnlyList<string> recommendedActions,
        CancellationToken cancellationToken)
    {
        var events = await QueryEventsAsync(filters, cancellationToken);
        var summary = BuildSummary(events);
        var pivots = BuildPivots(filters.ViewType, events);
        var relatedCases = await QueryRelatedCasesAsync(events.Select(e => e.EventId).ToArray(), cancellationToken);

        return Ok(new InvestigationViewDto(
            filters.ViewType,
            title,
            scopeDescription,
            summary,
            pivots,
            events,
            relatedCases,
            analystQuestions,
            recommendedActions));
    }

    private async Task<IReadOnlyList<InvestigationTimelineEventDto>> QueryEventsAsync(InvestigationFilters filters, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var where = new List<string>();
        var parameters = new List<(string Name, object? Value)>();

        if (filters.Categories.Count > 0)
        {
            var categoryParts = new List<string>();
            for (var i = 0; i < filters.Categories.Count; i++)
            {
                var name = $"Category{i}";
                categoryParts.Add($"LOWER(COALESCE(\"EventCategory\", '')) LIKE @{name}");
                parameters.Add((name, $"%{filters.Categories[i].ToLowerInvariant()}%"));
            }

            where.Add("(" + string.Join(" OR ", categoryParts) + ")");
        }

        AddEquals(where, parameters, "User", filters.User);
        AddEquals(where, parameters, "Mailbox", filters.Mailbox);
        AddEquals(where, parameters, "Domain", filters.Domain);
        AddEquals(where, parameters, "SourceIp", filters.SourceIp);
        AddEquals(where, parameters, "DestinationIp", filters.DestinationIp);
        AddEquals(where, parameters, "Host", filters.Host);
        AddEquals(where, parameters, "ProcessName", filters.ProcessName);
        AddEquals(where, parameters, "CloudTenantId", filters.CloudTenantId);
        AddEquals(where, parameters, "CloudResourceId", filters.CloudResourceId);
        AddEquals(where, parameters, "Action", filters.Action);

        if (!string.IsNullOrWhiteSpace(filters.ExtraQuery))
        {
            where.Add("(\"Message\" ILIKE @ExtraQuery OR \"RawJson\" ILIKE @ExtraQuery OR COALESCE(\"CommandLine\", '') ILIKE @ExtraQuery OR COALESCE(\"FileHashSha256\", '') ILIKE @ExtraQuery)");
            parameters.Add(("ExtraQuery", $"%{filters.ExtraQuery.Trim()}%"));
        }

        if (filters.FromUtc.HasValue)
        {
            where.Add("\"TimestampUtc\" >= @FromUtc");
            parameters.Add(("FromUtc", filters.FromUtc.Value.ToUniversalTime()));
        }

        if (filters.ToUtc.HasValue)
        {
            where.Add("\"TimestampUtc\" <= @ToUtc");
            parameters.Add(("ToUtc", filters.ToUtc.Value.ToUniversalTime()));
        }

        parameters.Add(("Take", Math.Clamp(filters.Take, 1, 1000)));

        var sql =
            """
            SELECT "Id", "TimestampUtc", COALESCE("EventCategory", ''), "EventType", "Severity", "User", "Host", "SourceIp", "DestinationIp", "Domain", "Action", "Outcome", "Message"
            FROM "SiemEvents"
            """ +
            (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : string.Empty) +
            " ORDER BY \"TimestampUtc\" DESC LIMIT @Take";

        await using var command = CreateCommand(connection, sql, parameters.ToArray());
        var results = new List<InvestigationTimelineEventDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new InvestigationTimelineEventDto(
                reader.GetGuid(0),
                reader.GetDateTime(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                GetNullableString(reader, 5),
                reader.GetString(6),
                GetNullableString(reader, 7),
                GetNullableString(reader, 8),
                GetNullableString(reader, 9),
                GetNullableString(reader, 10),
                GetNullableString(reader, 11),
                reader.GetString(12)));
        }

        return results;
    }

    private async Task<IReadOnlyList<InvestigationRelatedCaseDto>> QueryRelatedCasesAsync(IReadOnlyList<Guid> eventIds, CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return [];
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var eventParameterNames = eventIds.Select((_, index) => $"@EventId{index}").ToArray();
        var parameters = eventIds.Select((id, index) => ($"EventId{index}", (object?)id)).ToList();

        var sql =
            $"""
            SELECT DISTINCT c."Id", c."CaseNumber", c."Title", c."Severity", c."Status", c."UpdatedAtUtc"
            FROM "Cases" c
            INNER JOIN "CaseEventLinks" cel ON cel."CaseId" = c."Id"
            WHERE cel."EventId" IN ({string.Join(",", eventParameterNames)})
            ORDER BY c."UpdatedAtUtc" DESC
            LIMIT 100
            """;

        await using var command = CreateCommand(connection, sql, parameters.ToArray());
        var results = new List<InvestigationRelatedCaseDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new InvestigationRelatedCaseDto(reader.GetGuid(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4), reader.GetDateTime(5)));
        }

        return results;
    }

    private static InvestigationViewSummaryDto BuildSummary(IReadOnlyList<InvestigationTimelineEventDto> events) =>
        new(
            events.Count,
            events.Count(e => IsHighOrCritical(e.Severity)),
            events.Select(e => e.User).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            events.Select(e => e.Host).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            events.Select(e => e.SourceIp).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            events.Select(e => e.DestinationIp).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            events.Select(e => e.Domain).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            0,
            events.Count == 0 ? null : events.Min(e => e.TimestampUtc),
            events.Count == 0 ? null : events.Max(e => e.TimestampUtc));

    private static IReadOnlyList<InvestigationPivotDto> BuildPivots(string viewType, IReadOnlyList<InvestigationTimelineEventDto> events)
    {
        var pivots = new List<InvestigationPivotDto>();
        AddPivotGroup(pivots, viewType, "user", "User", events.Select(e => e.User));
        AddPivotGroup(pivots, viewType, "host", "Host", events.Select(e => e.Host));
        AddPivotGroup(pivots, viewType, "sourceIp", "Source IP", events.Select(e => e.SourceIp));
        AddPivotGroup(pivots, viewType, "destinationIp", "Destination IP", events.Select(e => e.DestinationIp));
        AddPivotGroup(pivots, viewType, "domain", "Domain", events.Select(e => e.Domain));
        AddPivotGroup(pivots, viewType, "action", "Action", events.Select(e => e.Action));
        return pivots.OrderByDescending(p => p.EventCount).Take(40).ToArray();
    }

    private static void AddPivotGroup(List<InvestigationPivotDto> pivots, string viewType, string queryName, string label, IEnumerable<string?> values)
    {
        foreach (var group in values.Where(v => !string.IsNullOrWhiteSpace(v)).GroupBy(v => v!, StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).Take(8))
        {
            var value = group.Key;
            pivots.Add(new InvestigationPivotDto(queryName, label, value, group.Count(), $"/api/investigations/views/{viewType}?{queryName}={Uri.EscapeDataString(value)}"));
        }
    }

    private static void AddEquals(List<string> where, List<(string Name, object? Value)> parameters, string columnName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        where.Add($"\"{columnName}\" = @{columnName}");
        parameters.Add((columnName, value.Trim()));
    }

    private static DbCommand CreateCommand(DbConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        return command;
    }

    private static string? GetNullableString(DbDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);

    private static bool IsHighOrCritical(string severity) =>
        string.Equals(severity, "High", StringComparison.OrdinalIgnoreCase) || string.Equals(severity, "Critical", StringComparison.OrdinalIgnoreCase);

    private static readonly string[] EmailQuestions =
    [
        "Who sent or received the suspicious message?",
        "Were there sign-ins, forwarding rules or inbox-rule changes around the same time?",
        "Which URLs, attachments or file hashes are related?",
        "Did the mailbox action succeed, fail or get blocked?",
        "What other users or mailboxes show related activity?"
    ];

    private static readonly string[] EmailActions =
    [
        "Pivot by sender, recipient, mailbox and domain.",
        "Review authentication events for the user around the message timestamp.",
        "Create a case and link related SIEM events, URLs and attachment hashes.",
        "Check mailbox forwarding, inbox rules and OAuth grant events."
    ];

    private static readonly string[] CloudQuestions =
    [
        "Which identity performed the action?",
        "From which IP and tenant was the action observed?",
        "Which cloud resource, app or role was targeted?",
        "Was the action successful?",
        "What related privilege, access-key or policy changes occurred nearby?"
    ];

    private static readonly string[] CloudActions =
    [
        "Pivot by user, source IP, action, tenant and resource.",
        "Review role assignment and access-key creation events.",
        "Check whether the same IP touched other users or resources.",
        "Create a case for suspicious successful administrative action."
    ];

    private static readonly string[] WindowsQuestions =
    [
        "Which host and user are involved?",
        "Which process or command line triggered the investigation?",
        "Was there related PowerShell, service creation, scheduled task or registry persistence?",
        "Did the host make outbound network connections afterwards?",
        "Is any file hash or destination known bad?"
    ];

    private static readonly string[] WindowsActions =
    [
        "Pivot by host, user, process, source IP and destination IP.",
        "Open raw JSON and parsed fields for suspicious process events.",
        "Link suspicious endpoint events to a case timeline.",
        "Check related IOC matches and recent telemetry from the same host."
    ];

    private sealed record InvestigationFilters(
        string ViewType,
        string? User,
        string? Mailbox,
        string? Domain,
        string? SourceIp,
        string? DestinationIp,
        string? Host,
        string? ProcessName,
        string? CloudTenantId,
        string? CloudResourceId,
        string? Action,
        string? ExtraQuery,
        IReadOnlyList<string> Categories,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int Take);
}
