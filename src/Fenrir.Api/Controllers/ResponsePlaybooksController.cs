using System.Data;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/response-playbooks")]
public sealed class ResponsePlaybooksController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResponsePlaybookDto>>> List(CancellationToken cancellationToken)
    {
        await EnsureDefaultPlaybooksAsync(cancellationToken);
        return Ok(await ReadPlaybooksAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ResponsePlaybookDto>> Create(ResponsePlaybookCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO "ResponsePlaybooks" ("Id", "Name", "Description", "Category", "Severity", "TriggerType", "MitreTactic", "MitreTechnique", "IsEnabled", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @Name, @Description, @Category, @Severity, @TriggerType, @MitreTactic, @MitreTechnique, @IsEnabled, @CreatedAtUtc, @UpdatedAtUtc)
            """, cancellationToken,
            ("Id", id), ("Name", request.Name), ("Description", request.Description), ("Category", request.Category), ("Severity", request.Severity),
            ("TriggerType", request.TriggerType), ("MitreTactic", request.MitreTactic), ("MitreTechnique", request.MitreTechnique), ("IsEnabled", request.IsEnabled),
            ("CreatedAtUtc", now), ("UpdatedAtUtc", now));

        var created = (await ReadPlaybooksAsync(cancellationToken)).First(item => item.Id == id);
        return Created($"/api/response-playbooks/{id}", created);
    }

    [HttpPost("{id:guid}/steps")]
    public async Task<ActionResult<ResponsePlaybookDto>> AddStep(Guid id, ResponsePlaybookStepCreateRequest request, CancellationToken cancellationToken)
    {
        await ExecuteAsync("""
            INSERT INTO "ResponsePlaybookSteps" ("Id", "PlaybookId", "Title", "Description", "ActionType", "TargetType", "CommandPreview", "IntegrationKey", "RequiresApproval", "SortOrder")
            VALUES (@Id, @PlaybookId, @Title, @Description, @ActionType, @TargetType, @CommandPreview, @IntegrationKey, @RequiresApproval, @SortOrder)
            """, cancellationToken,
            ("Id", Guid.NewGuid()), ("PlaybookId", id), ("Title", request.Title), ("Description", request.Description), ("ActionType", request.ActionType),
            ("TargetType", request.TargetType), ("CommandPreview", request.CommandPreview), ("IntegrationKey", request.IntegrationKey),
            ("RequiresApproval", request.RequiresApproval), ("SortOrder", request.SortOrder));

        var playbook = (await ReadPlaybooksAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);
        return playbook is null ? NotFound() : Ok(playbook);
    }

    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<ResponsePlaybookRunDto>>> ListRuns(CancellationToken cancellationToken)
    {
        return Ok(await ReadRunsAsync(cancellationToken));
    }

    [HttpPost("runs")]
    public async Task<ActionResult<ResponsePlaybookRunDto>> StartRun(ResponsePlaybookRunRequest request, CancellationToken cancellationToken)
    {
        var playbook = (await ReadPlaybooksAsync(cancellationToken)).FirstOrDefault(item => item.Id == request.PlaybookId);
        if (playbook is null) return NotFound("Playbook not found.");

        var runId = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO "ResponsePlaybookRuns" ("Id", "PlaybookId", "PlaybookName", "CaseId", "AlertId", "EventId", "Status", "StartedBy", "Notes", "StartedAtUtc", "CompletedAtUtc")
            VALUES (@Id, @PlaybookId, @PlaybookName, @CaseId, @AlertId, @EventId, @Status, @StartedBy, @Notes, @StartedAtUtc, @CompletedAtUtc)
            """, cancellationToken,
            ("Id", runId), ("PlaybookId", playbook.Id), ("PlaybookName", playbook.Name), ("CaseId", request.CaseId), ("AlertId", request.AlertId),
            ("EventId", request.EventId), ("Status", "Started"), ("StartedBy", request.StartedBy), ("Notes", request.Notes), ("StartedAtUtc", DateTime.UtcNow), ("CompletedAtUtc", null));

        foreach (var step in playbook.Steps.OrderBy(step => step.SortOrder))
        {
            await ExecuteAsync("""
                INSERT INTO "ResponsePlaybookRunSteps" ("Id", "RunId", "PlaybookStepId", "Title", "Status", "Result", "ExecutedBy", "ExecutedAtUtc", "RequiresApproval", "SortOrder")
                VALUES (@Id, @RunId, @PlaybookStepId, @Title, @Status, @Result, @ExecutedBy, @ExecutedAtUtc, @RequiresApproval, @SortOrder)
                """, cancellationToken,
                ("Id", Guid.NewGuid()), ("RunId", runId), ("PlaybookStepId", step.Id), ("Title", step.Title), ("Status", "Pending"),
                ("Result", null), ("ExecutedBy", null), ("ExecutedAtUtc", null), ("RequiresApproval", step.RequiresApproval), ("SortOrder", step.SortOrder));
        }

        var run = (await ReadRunsAsync(cancellationToken)).First(item => item.Id == runId);
        return Created($"/api/response-playbooks/runs/{runId}", run);
    }

    [HttpPatch("runs/{runId:guid}/steps/{stepId:guid}")]
    public async Task<ActionResult<ResponsePlaybookRunDto>> UpdateRunStep(Guid runId, Guid stepId, ResponsePlaybookStepUpdateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await ExecuteAsync("""
            UPDATE "ResponsePlaybookRunSteps"
            SET "Status" = @Status, "Result" = @Result, "ExecutedBy" = @ExecutedBy, "ExecutedAtUtc" = @ExecutedAtUtc
            WHERE "RunId" = @RunId AND "Id" = @StepId
            """, cancellationToken,
            ("RunId", runId), ("StepId", stepId), ("Status", request.Status), ("Result", request.Result), ("ExecutedBy", request.ExecutedBy), ("ExecutedAtUtc", now));

        var steps = await ReadRunStepsAsync(runId, cancellationToken);
        var complete = steps.Count > 0 && steps.All(step => step.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || step.Status.Equals("Skipped", StringComparison.OrdinalIgnoreCase));
        if (complete)
        {
            await ExecuteAsync("UPDATE \"ResponsePlaybookRuns\" SET \"Status\" = @Status, \"CompletedAtUtc\" = @CompletedAtUtc WHERE \"Id\" = @Id", cancellationToken,
                ("Id", runId), ("Status", "Completed"), ("CompletedAtUtc", now));
        }

        var run = (await ReadRunsAsync(cancellationToken)).FirstOrDefault(item => item.Id == runId);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpPost("recommendations")]
    public async Task<ActionResult<ResponseRecommendationDto>> Recommend(ResponseRecommendationRequest request, CancellationToken cancellationToken)
    {
        await EnsureDefaultPlaybooksAsync(cancellationToken);
        var playbooks = await ReadPlaybooksAsync(cancellationToken);
        var alertTitle = string.Empty;
        var alertSeverity = "Medium";

        if (request.AlertId.HasValue)
        {
            await using var command = await CreateCommandAsync("SELECT \"Title\", \"Severity\", \"Description\" FROM \"CorrelationAlerts\" WHERE \"Id\" = @Id", cancellationToken, ("Id", request.AlertId.Value));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                alertTitle = reader.GetString(0);
                alertSeverity = reader.GetString(1);
            }
        }

        var recommended = playbooks.Where(item => item.IsEnabled).Where(item =>
            string.IsNullOrWhiteSpace(alertTitle)
            || alertTitle.Contains(item.Category, StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("contain", StringComparison.OrdinalIgnoreCase)
            || item.Severity.Equals(alertSeverity, StringComparison.OrdinalIgnoreCase)).Take(3).ToList();

        if (!recommended.Any()) recommended = playbooks.Where(item => item.IsEnabled).Take(3).ToList();

        return Ok(new ResponseRecommendationDto(
            string.IsNullOrWhiteSpace(alertTitle) ? "Recommended response actions" : $"Recommended response for {alertTitle}",
            "Recommendations are based on alert context, severity and enabled playbook categories. Execution remains analyst-approved.",
            alertSeverity,
            recommended.Select(item => item.Id).ToList(),
            ["Validate the alert evidence before containment.", "Preserve relevant logs, raw JSON and case notes.", "Use approved playbook steps; avoid destructive actions without authorisation."]));
    }

    private async Task EnsureDefaultPlaybooksAsync(CancellationToken cancellationToken)
    {
        var count = Convert.ToInt32(await ScalarAsync("SELECT COUNT(*) FROM \"ResponsePlaybooks\"", cancellationToken));
        if (count > 0) return;

        var defaults = new[]
        {
            new ResponsePlaybookCreateRequest("Account compromise containment", "Validate, contain and preserve evidence for suspected identity compromise.", "identity", "High", "correlation_alert", "Credential Access", "T1078"),
            new ResponsePlaybookCreateRequest("Endpoint malware triage", "Triage a suspected malware or suspicious PowerShell event without automatic destructive action.", "endpoint", "Critical", "correlation_alert", "Execution", "T1059.001"),
            new ResponsePlaybookCreateRequest("Mailbox rule abuse response", "Investigate suspicious inbox rule or forwarding rule activity after login.", "email", "High", "correlation_alert", "Collection", "T1114")
        };

        foreach (var item in defaults)
        {
            var created = await Create(item, cancellationToken);
            var dto = ((CreatedResult)created.Result!).Value as ResponsePlaybookDto;
            if (dto is null) continue;

            var steps = item.Category switch
            {
                "identity" => new[]
                {
                    new ResponsePlaybookStepCreateRequest("Validate sign-in evidence", "Review source IP, user, MFA outcome, conditional access result and related events.", "manual", "analyst", null, null, true, 10),
                    new ResponsePlaybookStepCreateRequest("Disable or reset account", "Use your identity provider to disable the user or force password reset after approval.", "approval_required", "identity", "Disable account / revoke sessions in identity provider", "manual_identity", true, 20),
                    new ResponsePlaybookStepCreateRequest("Preserve evidence", "Attach sign-in logs, alert graph and case notes to the investigation.", "manual", "case", null, null, false, 30)
                },
                "endpoint" => new[]
                {
                    new ResponsePlaybookStepCreateRequest("Validate endpoint event chain", "Review process, command line, parent process, destination IP and file hash.", "manual", "analyst", null, null, true, 10),
                    new ResponsePlaybookStepCreateRequest("Isolate endpoint", "Use approved EDR/RMM tooling to isolate the endpoint if compromise is likely.", "approval_required", "endpoint", "Isolate host in EDR/RMM", "manual_endpoint", true, 20),
                    new ResponsePlaybookStepCreateRequest("Collect forensic bundle", "Collect running processes, network connections, persistence, autoruns and relevant event logs.", "manual", "endpoint", "Run Fenrir evidence collection workflow", "fenrir_agent", true, 30)
                },
                _ => new[]
                {
                    new ResponsePlaybookStepCreateRequest("Review mailbox changes", "Inspect inbox rules, forwarding settings, OAuth grants and recent sign-ins.", "manual", "mailbox", null, null, true, 10),
                    new ResponsePlaybookStepCreateRequest("Remove malicious rules", "After approval, remove suspicious forwarding or inbox rules using the mail platform.", "approval_required", "mailbox", "Remove suspicious mailbox rules", "manual_mailbox", true, 20),
                    new ResponsePlaybookStepCreateRequest("Notify user and preserve evidence", "Record screenshots/log exports and notify the user or customer contact.", "manual", "case", null, null, false, 30)
                }
            };

            foreach (var step in steps) await AddStep(dto.Id, step, cancellationToken);
        }
    }

    private async Task<List<ResponsePlaybookDto>> ReadPlaybooksAsync(CancellationToken cancellationToken)
    {
        var playbooks = new List<ResponsePlaybookDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"ResponsePlaybooks\" ORDER BY \"CreatedAtUtc\"", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = GetGuid(reader, "Id");
            playbooks.Add(new ResponsePlaybookDto(id, GetString(reader, "Name"), GetString(reader, "Description"), GetString(reader, "Category"), GetString(reader, "Severity"), GetString(reader, "TriggerType"), GetNullableString(reader, "MitreTactic"), GetNullableString(reader, "MitreTechnique"), GetBool(reader, "IsEnabled"), GetDate(reader, "CreatedAtUtc"), GetDate(reader, "UpdatedAtUtc"), await ReadStepsAsync(id, cancellationToken)));
        }
        return playbooks;
    }

    private async Task<List<ResponsePlaybookStepDto>> ReadStepsAsync(Guid playbookId, CancellationToken cancellationToken)
    {
        var steps = new List<ResponsePlaybookStepDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"ResponsePlaybookSteps\" WHERE \"PlaybookId\" = @PlaybookId ORDER BY \"SortOrder\"", cancellationToken, ("PlaybookId", playbookId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            steps.Add(new ResponsePlaybookStepDto(GetGuid(reader, "Id"), GetGuid(reader, "PlaybookId"), GetString(reader, "Title"), GetString(reader, "Description"), GetString(reader, "ActionType"), GetString(reader, "TargetType"), GetNullableString(reader, "CommandPreview"), GetNullableString(reader, "IntegrationKey"), GetBool(reader, "RequiresApproval"), GetInt(reader, "SortOrder")));
        }
        return steps;
    }

    private async Task<List<ResponsePlaybookRunDto>> ReadRunsAsync(CancellationToken cancellationToken)
    {
        var runs = new List<ResponsePlaybookRunDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"ResponsePlaybookRuns\" ORDER BY \"StartedAtUtc\" DESC LIMIT 100", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = GetGuid(reader, "Id");
            runs.Add(new ResponsePlaybookRunDto(id, GetGuid(reader, "PlaybookId"), GetString(reader, "PlaybookName"), GetNullableGuid(reader, "CaseId"), GetNullableGuid(reader, "AlertId"), GetNullableGuid(reader, "EventId"), GetString(reader, "Status"), GetString(reader, "StartedBy"), GetNullableString(reader, "Notes"), GetDate(reader, "StartedAtUtc"), GetNullableDate(reader, "CompletedAtUtc"), await ReadRunStepsAsync(id, cancellationToken)));
        }
        return runs;
    }

    private async Task<List<ResponsePlaybookRunStepDto>> ReadRunStepsAsync(Guid runId, CancellationToken cancellationToken)
    {
        var steps = new List<ResponsePlaybookRunStepDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"ResponsePlaybookRunSteps\" WHERE \"RunId\" = @RunId ORDER BY \"SortOrder\"", cancellationToken, ("RunId", runId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            steps.Add(new ResponsePlaybookRunStepDto(GetGuid(reader, "Id"), GetGuid(reader, "RunId"), GetGuid(reader, "PlaybookStepId"), GetString(reader, "Title"), GetString(reader, "Status"), GetNullableString(reader, "Result"), GetNullableString(reader, "ExecutedBy"), GetNullableDate(reader, "ExecutedAtUtc"), GetBool(reader, "RequiresApproval"), GetInt(reader, "SortOrder")));
        }
        return steps;
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

    private static Guid GetGuid(IDataRecord record, string name) => record.GetGuid(record.GetOrdinal(name));
    private static Guid? GetNullableGuid(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetGuid(record.GetOrdinal(name));
    private static string GetString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? string.Empty : record.GetString(record.GetOrdinal(name));
    private static string? GetNullableString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetString(record.GetOrdinal(name));
    private static bool GetBool(IDataRecord record, string name) => record.GetBoolean(record.GetOrdinal(name));
    private static int GetInt(IDataRecord record, string name) => record.GetInt32(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
    private static DateTime? GetNullableDate(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetDateTime(record.GetOrdinal(name));
}
