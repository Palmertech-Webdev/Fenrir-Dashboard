using System.Data;
using System.Text.Json;
using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Infrastructure.Correlation;

public sealed class EfCorrelationService(FenrirDbContext dbContext) : ICorrelationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CorrelationRuleDto>> ListRulesAsync(CancellationToken cancellationToken)
    {
        await EnsureDefaultRulesAsync(cancellationToken);
        return await ReadRulesAsync(cancellationToken);
    }

    public async Task<CorrelationRuleDto> CreateRuleAsync(CorrelationRuleCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dto = new CorrelationRuleDto(
            Guid.NewGuid(),
            request.Name.Trim(),
            request.Description.Trim(),
            NormaliseSeverity(request.Severity),
            request.Enabled,
            string.IsNullOrWhiteSpace(request.RuleType) ? "custom" : request.RuleType.Trim(),
            request.QueryDefinition?.Trim() ?? "custom",
            Math.Max(1, request.TimeWindowMinutes),
            request.GroupByFields?.Trim() ?? string.Empty,
            Math.Max(1, request.Threshold),
            request.MitreTactic,
            request.MitreTechnique,
            now,
            now);

        await ExecuteAsync("""
            INSERT INTO "CorrelationRules" ("Id", "Name", "Description", "Severity", "Enabled", "RuleType", "QueryDefinition", "TimeWindowMinutes", "GroupByFields", "Threshold", "MitreTactic", "MitreTechnique", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @Name, @Description, @Severity, @Enabled, @RuleType, @QueryDefinition, @TimeWindowMinutes, @GroupByFields, @Threshold, @MitreTactic, @MitreTechnique, @CreatedAtUtc, @UpdatedAtUtc)
            """, cancellationToken,
            ("Id", dto.Id), ("Name", dto.Name), ("Description", dto.Description), ("Severity", dto.Severity), ("Enabled", dto.Enabled),
            ("RuleType", dto.RuleType), ("QueryDefinition", dto.QueryDefinition), ("TimeWindowMinutes", dto.TimeWindowMinutes), ("GroupByFields", dto.GroupByFields),
            ("Threshold", dto.Threshold), ("MitreTactic", dto.MitreTactic), ("MitreTechnique", dto.MitreTechnique), ("CreatedAtUtc", dto.CreatedAtUtc), ("UpdatedAtUtc", dto.UpdatedAtUtc));

        return dto;
    }

    public async Task<CorrelationRuleDto?> UpdateRuleAsync(Guid id, CorrelationRuleUpdateRequest request, CancellationToken cancellationToken)
    {
        var current = (await ReadRulesAsync(cancellationToken)).FirstOrDefault(rule => rule.Id == id);
        if (current is null)
        {
            return null;
        }

        var updated = current with
        {
            Name = request.Name?.Trim() ?? current.Name,
            Description = request.Description?.Trim() ?? current.Description,
            Severity = request.Severity is null ? current.Severity : NormaliseSeverity(request.Severity),
            Enabled = request.Enabled ?? current.Enabled,
            RuleType = request.RuleType?.Trim() ?? current.RuleType,
            QueryDefinition = request.QueryDefinition?.Trim() ?? current.QueryDefinition,
            TimeWindowMinutes = request.TimeWindowMinutes.HasValue ? Math.Max(1, request.TimeWindowMinutes.Value) : current.TimeWindowMinutes,
            GroupByFields = request.GroupByFields?.Trim() ?? current.GroupByFields,
            Threshold = request.Threshold.HasValue ? Math.Max(1, request.Threshold.Value) : current.Threshold,
            MitreTactic = request.MitreTactic ?? current.MitreTactic,
            MitreTechnique = request.MitreTechnique ?? current.MitreTechnique,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await ExecuteAsync("""
            UPDATE "CorrelationRules"
            SET "Name" = @Name, "Description" = @Description, "Severity" = @Severity, "Enabled" = @Enabled, "RuleType" = @RuleType,
                "QueryDefinition" = @QueryDefinition, "TimeWindowMinutes" = @TimeWindowMinutes, "GroupByFields" = @GroupByFields, "Threshold" = @Threshold,
                "MitreTactic" = @MitreTactic, "MitreTechnique" = @MitreTechnique, "UpdatedAtUtc" = @UpdatedAtUtc
            WHERE "Id" = @Id
            """, cancellationToken,
            ("Id", updated.Id), ("Name", updated.Name), ("Description", updated.Description), ("Severity", updated.Severity), ("Enabled", updated.Enabled),
            ("RuleType", updated.RuleType), ("QueryDefinition", updated.QueryDefinition), ("TimeWindowMinutes", updated.TimeWindowMinutes), ("GroupByFields", updated.GroupByFields),
            ("Threshold", updated.Threshold), ("MitreTactic", updated.MitreTactic), ("MitreTechnique", updated.MitreTechnique), ("UpdatedAtUtc", updated.UpdatedAtUtc));

        return updated;
    }

    public async Task<IReadOnlyList<CorrelationAlertDto>> ListAlertsAsync(CancellationToken cancellationToken)
    {
        return await ReadAlertsAsync(cancellationToken);
    }

    public async Task<CorrelationRunResponse> RunAsync(CorrelationRunRequest request, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        await EnsureDefaultRulesAsync(cancellationToken);

        var rules = await ReadRulesAsync(cancellationToken);
        if (request.RuleId.HasValue)
        {
            rules = rules.Where(rule => rule.Id == request.RuleId.Value).ToList();
        }

        rules = rules.Where(rule => rule.Enabled).ToList();
        var lookback = DateTime.UtcNow.AddMinutes(-Math.Max(5, request.LookbackMinutes));
        var take = Math.Clamp(request.Take, 50, 5000);
        var events = await dbContext.SiemEvents
            .AsNoTracking()
            .Where(securityEvent => securityEvent.TimestampUtc >= lookback)
            .OrderByDescending(securityEvent => securityEvent.TimestampUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        var created = new List<CorrelationAlertDto>();
        foreach (var rule in rules)
        {
            created.AddRange(await EvaluateRuleAsync(rule, events, cancellationToken));
        }

        var completed = DateTime.UtcNow;
        return new CorrelationRunResponse(started, completed, rules.Count, created.Count, created);
    }

    public async Task<EntityGraphResponse> BuildEntityGraphAsync(Guid? alertId, int lookbackMinutes, CancellationToken cancellationToken)
    {
        var events = await ResolveGraphEventsAsync(alertId, lookbackMinutes, cancellationToken);
        var nodes = new Dictionary<string, EntityGraphNodeDto>(StringComparer.OrdinalIgnoreCase);
        var edges = new Dictionary<string, EntityGraphEdgeDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in events)
        {
            AddNode(nodes, "event", item.Id.ToString(), item.EventType, 1);
            if (!string.IsNullOrWhiteSpace(item.User)) Link(nodes, edges, "user", item.User, "event", item.Id.ToString(), "generated", 1);
            if (!string.IsNullOrWhiteSpace(item.Host)) Link(nodes, edges, "host", item.Host, "event", item.Id.ToString(), "reported", 1);
            if (!string.IsNullOrWhiteSpace(item.SourceIp)) Link(nodes, edges, "ip", item.SourceIp, "event", item.Id.ToString(), "source", 1);
            if (!string.IsNullOrWhiteSpace(item.DestinationIp)) Link(nodes, edges, "event", item.Id.ToString(), "ip", item.DestinationIp, "connected_to", 1);
            if (!string.IsNullOrWhiteSpace(item.Domain)) Link(nodes, edges, "event", item.Id.ToString(), "domain", item.Domain, "referenced", 1);
            if (!string.IsNullOrWhiteSpace(item.FileHashSha256)) Link(nodes, edges, "event", item.Id.ToString(), "hash", item.FileHashSha256, "observed_hash", 1);
            if (!string.IsNullOrWhiteSpace(item.ProcessName)) Link(nodes, edges, "process", item.ProcessName, "event", item.Id.ToString(), "process", 1);
            if (!string.IsNullOrWhiteSpace(item.CloudResourceId)) Link(nodes, edges, "event", item.Id.ToString(), "cloud_resource", item.CloudResourceId, "modified", 1);
        }

        var narrative = BuildNarrative(events, nodes.Values.ToList());
        return new EntityGraphResponse(nodes.Values.OrderByDescending(node => node.Weight).Take(150).ToList(), edges.Values.OrderByDescending(edge => edge.Weight).Take(250).ToList(), narrative);
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> EvaluateRuleAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        return rule.QueryDefinition switch
        {
            "multiple_failed_logins_then_success" => await MultipleFailedLoginsThenSuccessAsync(rule, events, cancellationToken),
            "same_ip_multiple_users" => await SameIpMultipleUsersAsync(rule, events, cancellationToken),
            "cloud_admin_role_assigned" => await MatchingActionAsync(rule, events, ["role", "admin", "privilege", "assignment"], cancellationToken),
            "malware_hash_observed" => await MalwareHashObservedAsync(rule, events, cancellationToken),
            "suspicious_powershell_network" => await SuspiciousPowerShellAsync(rule, events, cancellationToken),
            "new_inbox_rule_after_suspicious_login" => await InboxRuleAfterLoginAsync(rule, events, cancellationToken),
            _ => []
        };
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> MultipleFailedLoginsThenSuccessAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        var authEvents = events.Where(IsAuthEvent).Where(e => !string.IsNullOrWhiteSpace(e.User)).ToList();
        var alerts = new List<CorrelationAlertDto>();
        foreach (var group in authEvents.GroupBy(e => Normalise(e.User!)))
        {
            var failures = group.Count(IsFailure);
            var successes = group.Count(IsSuccess);
            if (failures >= rule.Threshold && successes > 0)
            {
                alerts.Add(await PersistAlertAsync(rule, $"Multiple failed logins followed by success for {group.Key}", $"{failures} failed authentication events and {successes} successful authentication event(s) were seen for the same user inside the lookback window.", group.ToList(), cancellationToken));
            }
        }
        return alerts;
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> SameIpMultipleUsersAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        var candidates = events.Where(e => !string.IsNullOrWhiteSpace(e.SourceIp) && !string.IsNullOrWhiteSpace(e.User)).GroupBy(e => e.SourceIp!);
        var alerts = new List<CorrelationAlertDto>();
        foreach (var group in candidates)
        {
            var users = group.Select(e => Normalise(e.User!)).Distinct().Count();
            if (users >= rule.Threshold)
            {
                alerts.Add(await PersistAlertAsync(rule, $"Single IP touched {users} users", $"Source IP {group.Key} appears across {users} distinct users and {group.Count()} events.", group.ToList(), cancellationToken));
            }
        }
        return alerts;
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> MatchingActionAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, IReadOnlyList<string> terms, CancellationToken cancellationToken)
    {
        var matches = events.Where(e => terms.Any(term => Contains(e.Action, term) || Contains(e.EventType, term) || Contains(e.Message, term))).ToList();
        if (!matches.Any()) return [];
        return [await PersistAlertAsync(rule, "Cloud or identity privilege change observed", "A privileged role, admin assignment or privilege-related cloud action was observed.", matches, cancellationToken)];
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> MalwareHashObservedAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        var matches = events.Where(e => !string.IsNullOrWhiteSpace(e.FileHashSha256) && (IsHighSeverity(e) || Contains(e.Message, "malware") || Contains(e.EventType, "malware"))).ToList();
        if (!matches.Any()) return [];
        return [await PersistAlertAsync(rule, "Malware hash observed on telemetry", "One or more endpoint/SIEM events contain a file hash associated with high severity or malware-like context.", matches, cancellationToken)];
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> SuspiciousPowerShellAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        var suspiciousTerms = new[] { "encodedcommand", "downloadstring", "invoke-webrequest", "iex", "http://", "https://" };
        var matches = events.Where(e => Contains(e.ProcessName, "powershell") || Contains(e.CommandLine, "powershell"))
            .Where(e => suspiciousTerms.Any(term => Contains(e.CommandLine, term) || Contains(e.Message, term)) || !string.IsNullOrWhiteSpace(e.DestinationIp))
            .ToList();
        if (!matches.Any()) return [];
        return [await PersistAlertAsync(rule, "Suspicious PowerShell with network or encoded behaviour", "PowerShell activity contains encoded/download/network indicators that should be investigated.", matches, cancellationToken)];
    }

    private async Task<IReadOnlyList<CorrelationAlertDto>> InboxRuleAfterLoginAsync(CorrelationRuleDto rule, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        var alerts = new List<CorrelationAlertDto>();
        foreach (var group in events.Where(e => !string.IsNullOrWhiteSpace(e.User)).GroupBy(e => Normalise(e.User!)))
        {
            var auth = group.Where(IsAuthEvent).Where(IsSuccess).ToList();
            var ruleChanges = group.Where(e => Contains(e.Action, "inbox") || Contains(e.EventType, "inbox") || Contains(e.Message, "inbox rule") || Contains(e.Message, "forwarding rule")).ToList();
            if (auth.Any() && ruleChanges.Any())
            {
                alerts.Add(await PersistAlertAsync(rule, $"Inbox rule activity after login for {group.Key}", "Mailbox rule or forwarding-rule activity was observed in the same user window as successful authentication.", auth.Concat(ruleChanges).ToList(), cancellationToken));
            }
        }
        return alerts;
    }

    private async Task<CorrelationAlertDto> PersistAlertAsync(CorrelationRuleDto rule, string title, string description, IReadOnlyList<SecurityEvent> events, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var entitySummary = BuildEntitySummary(events);
        var eventIds = events.Select(e => e.Id).Distinct().Take(100).ToList();
        var dto = new CorrelationAlertDto(Guid.NewGuid(), rule.Id, rule.Name, title, description, rule.Severity, "Open", events.Min(e => e.TimestampUtc), events.Max(e => e.TimestampUtc), now, eventIds, entitySummary, rule.MitreTactic, rule.MitreTechnique);

        await ExecuteAsync("""
            INSERT INTO "CorrelationAlerts" ("Id", "RuleId", "RuleName", "Title", "Description", "Severity", "Status", "FirstSeenUtc", "LastSeenUtc", "CreatedAtUtc", "EventIdsJson", "EntitySummaryJson", "MitreTactic", "MitreTechnique")
            VALUES (@Id, @RuleId, @RuleName, @Title, @Description, @Severity, @Status, @FirstSeenUtc, @LastSeenUtc, @CreatedAtUtc, @EventIdsJson, @EntitySummaryJson, @MitreTactic, @MitreTechnique)
            """, cancellationToken,
            ("Id", dto.Id), ("RuleId", dto.RuleId), ("RuleName", dto.RuleName), ("Title", dto.Title), ("Description", dto.Description), ("Severity", dto.Severity), ("Status", dto.Status),
            ("FirstSeenUtc", dto.FirstSeenUtc), ("LastSeenUtc", dto.LastSeenUtc), ("CreatedAtUtc", dto.CreatedAtUtc),
            ("EventIdsJson", JsonSerializer.Serialize(eventIds, JsonOptions)), ("EntitySummaryJson", JsonSerializer.Serialize(entitySummary, JsonOptions)),
            ("MitreTactic", dto.MitreTactic), ("MitreTechnique", dto.MitreTechnique));

        return dto;
    }

    private async Task EnsureDefaultRulesAsync(CancellationToken cancellationToken)
    {
        var count = await ScalarAsync("SELECT COUNT(*) FROM \"CorrelationRules\"", cancellationToken);
        if (Convert.ToInt32(count) > 0) return;

        var defaults = new[]
        {
            new CorrelationRuleCreateRequest("Multiple failed logins then success", "Same user has repeated failed logins followed by success.", "High", true, "built_in", "multiple_failed_logins_then_success", 60, "User", 3, "Credential Access", "T1110"),
            new CorrelationRuleCreateRequest("Same IP hits multiple users", "One source IP appears across multiple user accounts.", "Medium", true, "built_in", "same_ip_multiple_users", 120, "SourceIp", 3, "Initial Access", "T1078"),
            new CorrelationRuleCreateRequest("Cloud admin role assigned", "Cloud or identity admin/role assignment activity is observed.", "High", true, "built_in", "cloud_admin_role_assigned", 240, "User,Action", 1, "Privilege Escalation", "T1098"),
            new CorrelationRuleCreateRequest("Malware hash observed on endpoint", "High severity event includes a file hash or malware context.", "Critical", true, "built_in", "malware_hash_observed", 240, "FileHashSha256", 1, "Execution", "T1204"),
            new CorrelationRuleCreateRequest("Suspicious PowerShell with network connection", "PowerShell activity includes encoded/download/network behaviour.", "High", true, "built_in", "suspicious_powershell_network", 120, "Host,User", 1, "Execution", "T1059.001"),
            new CorrelationRuleCreateRequest("New inbox rule after suspicious login", "Mailbox rule activity appears near successful authentication.", "High", true, "built_in", "new_inbox_rule_after_suspicious_login", 240, "User,Mailbox", 1, "Collection", "T1114")
        };

        foreach (var rule in defaults)
        {
            await CreateRuleAsync(rule, cancellationToken);
        }
    }

    private async Task<List<CorrelationRuleDto>> ReadRulesAsync(CancellationToken cancellationToken)
    {
        var rows = new List<CorrelationRuleDto>();
        await WithCommandAsync("SELECT * FROM \"CorrelationRules\" ORDER BY \"CreatedAtUtc\"", async command =>
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new CorrelationRuleDto(GetGuid(reader, "Id"), GetString(reader, "Name"), GetString(reader, "Description"), GetString(reader, "Severity"), GetBool(reader, "Enabled"), GetString(reader, "RuleType"), GetString(reader, "QueryDefinition"), GetInt(reader, "TimeWindowMinutes"), GetString(reader, "GroupByFields"), GetInt(reader, "Threshold"), GetNullableString(reader, "MitreTactic"), GetNullableString(reader, "MitreTechnique"), GetDate(reader, "CreatedAtUtc"), GetDate(reader, "UpdatedAtUtc")));
            }
        }, cancellationToken);
        return rows;
    }

    private async Task<List<CorrelationAlertDto>> ReadAlertsAsync(CancellationToken cancellationToken)
    {
        var rows = new List<CorrelationAlertDto>();
        await WithCommandAsync("SELECT * FROM \"CorrelationAlerts\" ORDER BY \"CreatedAtUtc\" DESC LIMIT 250", async command =>
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var eventIds = JsonSerializer.Deserialize<List<Guid>>(GetString(reader, "EventIdsJson"), JsonOptions) ?? [];
                var entities = JsonSerializer.Deserialize<Dictionary<string, IReadOnlyList<string>>>(GetString(reader, "EntitySummaryJson"), JsonOptions) ?? [];
                rows.Add(new CorrelationAlertDto(GetGuid(reader, "Id"), GetNullableGuid(reader, "RuleId"), GetString(reader, "RuleName"), GetString(reader, "Title"), GetString(reader, "Description"), GetString(reader, "Severity"), GetString(reader, "Status"), GetDate(reader, "FirstSeenUtc"), GetDate(reader, "LastSeenUtc"), GetDate(reader, "CreatedAtUtc"), eventIds, entities, GetNullableString(reader, "MitreTactic"), GetNullableString(reader, "MitreTechnique")));
            }
        }, cancellationToken);
        return rows;
    }

    private async Task<IReadOnlyList<SecurityEvent>> ResolveGraphEventsAsync(Guid? alertId, int lookbackMinutes, CancellationToken cancellationToken)
    {
        if (alertId.HasValue)
        {
            var alert = (await ReadAlertsAsync(cancellationToken)).FirstOrDefault(item => item.Id == alertId.Value);
            if (alert is not null && alert.EventIds.Any())
            {
                return await dbContext.SiemEvents.AsNoTracking().Where(item => alert.EventIds.Contains(item.Id)).ToListAsync(cancellationToken);
            }
        }

        var lookback = DateTime.UtcNow.AddMinutes(-Math.Max(30, lookbackMinutes));
        return await dbContext.SiemEvents.AsNoTracking().Where(item => item.TimestampUtc >= lookback).OrderByDescending(item => item.TimestampUtc).Take(500).ToListAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> BuildEntitySummary(IEnumerable<SecurityEvent> events)
    {
        var list = events.ToList();
        return new Dictionary<string, IReadOnlyList<string>>
        {
            ["users"] = list.Select(e => e.User).WhereNotEmpty().Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList(),
            ["hosts"] = list.Select(e => e.Host).WhereNotEmpty().Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList(),
            ["ips"] = list.SelectMany(e => new[] { e.SourceIp, e.DestinationIp }).WhereNotEmpty().Distinct(StringComparer.OrdinalIgnoreCase).Take(30).ToList(),
            ["domains"] = list.Select(e => e.Domain).WhereNotEmpty().Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList(),
            ["hashes"] = list.Select(e => e.FileHashSha256).WhereNotEmpty().Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList(),
            ["actions"] = list.Select(e => e.Action).WhereNotEmpty().Distinct(StringComparer.OrdinalIgnoreCase).Take(20).ToList()
        };
    }

    private static List<string> BuildNarrative(IReadOnlyList<SecurityEvent> events, IReadOnlyList<EntityGraphNodeDto> nodes)
    {
        if (!events.Any()) return ["No recent event relationships are available for the selected scope."];
        var users = nodes.Count(n => n.Type == "user");
        var hosts = nodes.Count(n => n.Type == "host");
        var ips = nodes.Count(n => n.Type == "ip");
        return [$"Entity graph built from {events.Count} events.", $"Observed {users} users, {hosts} hosts and {ips} IP entities.", "Use this graph to identify shared infrastructure, repeated users, host concentration and IOC/event relationships before creating or expanding a case."];
    }

    private static void AddNode(IDictionary<string, EntityGraphNodeDto> nodes, string type, string value, string label, int weight)
    {
        var id = $"{type}:{value}";
        if (nodes.TryGetValue(id, out var existing))
        {
            nodes[id] = existing with { Weight = existing.Weight + weight };
            return;
        }
        nodes[id] = new EntityGraphNodeDto(id, label, type, weight);
    }

    private static void Link(IDictionary<string, EntityGraphNodeDto> nodes, IDictionary<string, EntityGraphEdgeDto> edges, string fromType, string fromValue, string toType, string toValue, string relationship, int weight)
    {
        AddNode(nodes, fromType, fromValue, fromValue, weight);
        AddNode(nodes, toType, toValue, toValue, weight);
        var from = $"{fromType}:{fromValue}";
        var to = $"{toType}:{toValue}";
        var id = $"{from}->{relationship}->{to}";
        if (edges.TryGetValue(id, out var existing)) edges[id] = existing with { Weight = existing.Weight + weight };
        else edges[id] = new EntityGraphEdgeDto(from, to, relationship, weight);
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await WithCommandAsync(sql, command =>
        {
            foreach (var parameter in parameters) AddParameter(command, parameter.Name, parameter.Value);
            return command.ExecuteNonQueryAsync(cancellationToken);
        }, cancellationToken);
    }

    private async Task<object?> ScalarAsync(string sql, CancellationToken cancellationToken)
    {
        object? result = null;
        await WithCommandAsync(sql, async command => result = await command.ExecuteScalarAsync(cancellationToken), cancellationToken);
        return result;
    }

    private async Task WithCommandAsync(string sql, Func<System.Data.Common.DbCommand, Task> action, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await action(command);
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = $"@{name}";
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static bool IsAuthEvent(SecurityEvent e) => Contains(e.EventCategory, "auth") || Contains(e.EventType, "login") || Contains(e.EventType, "signin") || Contains(e.Message, "login") || Contains(e.Message, "sign-in");
    private static bool IsFailure(SecurityEvent e) => Contains(e.Outcome, "fail") || Contains(e.Message, "fail");
    private static bool IsSuccess(SecurityEvent e) => Contains(e.Outcome, "success") || Contains(e.Message, "success");
    private static bool IsHighSeverity(SecurityEvent e) => string.Equals(e.Severity, "High", StringComparison.OrdinalIgnoreCase) || string.Equals(e.Severity, "Critical", StringComparison.OrdinalIgnoreCase);
    private static bool Contains(string? source, string value) => source?.Contains(value, StringComparison.OrdinalIgnoreCase) == true;
    private static string Normalise(string value) => value.Trim().ToLowerInvariant();
    private static string NormaliseSeverity(string value) => string.IsNullOrWhiteSpace(value) ? "Medium" : value.Trim();

    private static Guid GetGuid(IDataRecord record, string name) => record.GetGuid(record.GetOrdinal(name));
    private static Guid? GetNullableGuid(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetGuid(record.GetOrdinal(name));
    private static string GetString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? string.Empty : record.GetString(record.GetOrdinal(name));
    private static string? GetNullableString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetString(record.GetOrdinal(name));
    private static bool GetBool(IDataRecord record, string name) => record.GetBoolean(record.GetOrdinal(name));
    private static int GetInt(IDataRecord record, string name) => record.GetInt32(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
}

internal static class CorrelationEnumerableExtensions
{
    public static IEnumerable<string> WhereNotEmpty(this IEnumerable<string?> values) => values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!);
}
