using System.Data;
using System.Security.Cryptography;
using System.Text;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvestigationReportDto>>> ListReports(CancellationToken cancellationToken)
    {
        return Ok(await ReadReportsAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<InvestigationReportDto>> CreateReport(InvestigationReportCreateRequest request, CancellationToken cancellationToken)
    {
        var created = DateTime.UtcNow;
        var content = await BuildReportMarkdownAsync(request, created, cancellationToken);
        var hash = Sha256(content);
        var id = Guid.NewGuid();

        await ExecuteAsync("""
            INSERT INTO "InvestigationReports" ("Id", "Title", "ReportType", "Scope", "RequestedBy", "Status", "CaseId", "CreatedAtUtc", "CompletedAtUtc", "ContentMarkdown", "Sha256", "SignatureAlgorithm")
            VALUES (@Id, @Title, @ReportType, @Scope, @RequestedBy, @Status, @CaseId, @CreatedAtUtc, @CompletedAtUtc, @ContentMarkdown, @Sha256, @SignatureAlgorithm)
            """, cancellationToken,
            ("Id", id), ("Title", request.Title), ("ReportType", request.ReportType), ("Scope", request.Scope), ("RequestedBy", request.RequestedBy),
            ("Status", "Completed"), ("CaseId", request.CaseId), ("CreatedAtUtc", created), ("CompletedAtUtc", DateTime.UtcNow),
            ("ContentMarkdown", content), ("Sha256", hash), ("SignatureAlgorithm", "SHA256"));

        await SealInternalAsync("InvestigationReport", id.ToString(), content, "Automatic report seal", request.RequestedBy, cancellationToken);

        var report = (await ReadReportsAsync(cancellationToken)).First(item => item.Id == id);
        return Created($"/api/reports/{id}", report);
    }

    [HttpGet("{id:guid}/markdown")]
    public async Task<IActionResult> DownloadMarkdown(Guid id, CancellationToken cancellationToken)
    {
        var report = (await ReadReportsAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);
        if (report is null) return NotFound();
        return File(Encoding.UTF8.GetBytes(report.ContentMarkdown), "text/markdown", $"fenrir-report-{id}.md");
    }

    [HttpGet("evidence-integrity")]
    public async Task<ActionResult<IReadOnlyList<EvidenceIntegrityRecordDto>>> ListEvidenceIntegrity(CancellationToken cancellationToken)
    {
        return Ok(await ReadEvidenceRecordsAsync(cancellationToken));
    }

    [HttpPost("evidence-integrity")]
    public async Task<ActionResult<EvidenceIntegrityRecordDto>> SealEvidence(EvidenceSealRequest request, CancellationToken cancellationToken)
    {
        var id = await SealInternalAsync(request.EntityType, request.EntityId, request.Payload, request.Notes, request.SealedBy, cancellationToken);
        var record = (await ReadEvidenceRecordsAsync(cancellationToken)).First(item => item.Id == id);
        return Created($"/api/reports/evidence-integrity/{id}", record);
    }

    [HttpPost("evidence-integrity/verify")]
    public async Task<ActionResult<EvidenceVerifyResponse>> VerifyEvidence(EvidenceVerifyRequest request, CancellationToken cancellationToken)
    {
        var record = (await ReadEvidenceRecordsAsync(cancellationToken)).FirstOrDefault(item => item.Id == request.IntegrityRecordId);
        if (record is null) return NotFound("Evidence integrity record not found.");

        var actual = Sha256(request.Payload);
        var valid = string.Equals(actual, record.Sha256, StringComparison.OrdinalIgnoreCase);
        return Ok(new EvidenceVerifyResponse(record.Id, valid, record.Sha256, actual, valid ? "Payload matches the sealed SHA256 hash." : "Payload does not match the sealed SHA256 hash."));
    }

    private async Task<string> BuildReportMarkdownAsync(InvestigationReportCreateRequest request, DateTime created, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {request.Title}");
        builder.AppendLine();
        builder.AppendLine($"- Report type: {request.ReportType}");
        builder.AppendLine($"- Scope: {request.Scope ?? "Not specified"}");
        builder.AppendLine($"- Requested by: {request.RequestedBy}");
        builder.AppendLine($"- Created UTC: {created:u}");
        builder.AppendLine($"- Case ID: {request.CaseId?.ToString() ?? "Not linked"}");
        builder.AppendLine();

        builder.AppendLine("## Analyst summary");
        builder.AppendLine(string.IsNullOrWhiteSpace(request.AnalystSummary) ? "No analyst summary provided." : request.AnalystSummary);
        builder.AppendLine();

        if (request.IncludeFindings)
        {
            var findings = await dbContext.Findings.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).Take(25).ToListAsync(cancellationToken);
            builder.AppendLine("## Findings");
            if (findings.Count == 0) builder.AppendLine("No findings recorded.");
            foreach (var finding in findings)
            {
                builder.AppendLine($"- **{finding.Severity}** {finding.Title} [{finding.Status}] — {finding.Summary}");
            }
            builder.AppendLine();
        }

        if (request.IncludeSiemSummary)
        {
            var since = DateTime.UtcNow.AddHours(-24);
            var events = await dbContext.SiemEvents.AsNoTracking().Where(item => item.TimestampUtc >= since).OrderByDescending(item => item.TimestampUtc).Take(100).ToListAsync(cancellationToken);
            builder.AppendLine("## SIEM summary - last 24 hours");
            builder.AppendLine($"Total sampled events: {events.Count}");
            foreach (var group in events.GroupBy(item => item.Severity).OrderByDescending(item => item.Count()))
            {
                builder.AppendLine($"- {group.Key}: {group.Count()}");
            }
            builder.AppendLine();
            builder.AppendLine("### Recent high value events");
            foreach (var evt in events.Where(item => item.Severity is "High" or "Critical").Take(15))
            {
                builder.AppendLine($"- {evt.TimestampUtc:u} **{evt.Severity}** {evt.EventType} on {evt.Host}: {evt.Message}");
            }
            builder.AppendLine();
        }

        if (request.IncludeHuntRuns)
        {
            builder.AppendLine("## Hunt run summary");
            await AppendRawTableSummaryAsync(builder, "HuntRuns", "\"StartedAtUtc\"", ["HuntPackName", "Status", "Matches", "StartedAtUtc"], cancellationToken);
            builder.AppendLine();
        }

        if (request.IncludeResponseRuns)
        {
            builder.AppendLine("## Response playbook summary");
            await AppendRawTableSummaryAsync(builder, "ResponsePlaybookRuns", "\"StartedAtUtc\"", ["PlaybookName", "Status", "StartedBy", "StartedAtUtc"], cancellationToken);
            builder.AppendLine();
        }

        builder.AppendLine("## Conclusion");
        builder.AppendLine(string.IsNullOrWhiteSpace(request.Conclusion) ? "No conclusion provided." : request.Conclusion);
        builder.AppendLine();
        builder.AppendLine("## Integrity note");
        builder.AppendLine("This report is sealed with a SHA256 hash when generated. Recalculate the hash against the exact markdown content to verify integrity.");
        return builder.ToString();
    }

    private async Task AppendRawTableSummaryAsync(StringBuilder builder, string tableName, string orderColumn, string[] columns, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = await CreateCommandAsync($"SELECT * FROM \"{tableName}\" ORDER BY {orderColumn} DESC LIMIT 10", cancellationToken);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var hasRows = false;
            while (await reader.ReadAsync(cancellationToken))
            {
                hasRows = true;
                var values = columns.Select(column => $"{column}: {GetAny(reader, column)}");
                builder.AppendLine($"- {string.Join("; ", values)}");
            }
            if (!hasRows) builder.AppendLine("No records recorded.");
        }
        catch
        {
            builder.AppendLine($"{tableName} is not available in this database state.");
        }
    }

    private async Task<Guid> SealInternalAsync(string entityType, string entityId, string payload, string? notes, string sealedBy, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        await ExecuteAsync("""
            INSERT INTO "EvidenceIntegrityRecords" ("Id", "EntityType", "EntityId", "Sha256", "SignatureAlgorithm", "Notes", "SealedBy", "SealedAtUtc")
            VALUES (@Id, @EntityType, @EntityId, @Sha256, @SignatureAlgorithm, @Notes, @SealedBy, @SealedAtUtc)
            """, cancellationToken,
            ("Id", id), ("EntityType", entityType), ("EntityId", entityId), ("Sha256", Sha256(payload)), ("SignatureAlgorithm", "SHA256"),
            ("Notes", notes), ("SealedBy", sealedBy), ("SealedAtUtc", DateTime.UtcNow));
        return id;
    }

    private async Task<List<InvestigationReportDto>> ReadReportsAsync(CancellationToken cancellationToken)
    {
        var reports = new List<InvestigationReportDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"InvestigationReports\" ORDER BY \"CreatedAtUtc\" DESC LIMIT 100", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            reports.Add(new InvestigationReportDto(GetGuid(reader, "Id"), GetString(reader, "Title"), GetString(reader, "ReportType"), GetNullableString(reader, "Scope"), GetString(reader, "RequestedBy"), GetString(reader, "Status"), GetNullableGuid(reader, "CaseId"), GetDate(reader, "CreatedAtUtc"), GetNullableDate(reader, "CompletedAtUtc"), GetString(reader, "ContentMarkdown"), GetString(reader, "Sha256"), GetString(reader, "SignatureAlgorithm")));
        }
        return reports;
    }

    private async Task<List<EvidenceIntegrityRecordDto>> ReadEvidenceRecordsAsync(CancellationToken cancellationToken)
    {
        var records = new List<EvidenceIntegrityRecordDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"EvidenceIntegrityRecords\" ORDER BY \"SealedAtUtc\" DESC LIMIT 100", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new EvidenceIntegrityRecordDto(GetGuid(reader, "Id"), GetString(reader, "EntityType"), GetString(reader, "EntityId"), GetString(reader, "Sha256"), GetString(reader, "SignatureAlgorithm"), GetNullableString(reader, "Notes"), GetString(reader, "SealedBy"), GetDate(reader, "SealedAtUtc")));
        }
        return records;
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static string Sha256(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? string.Empty));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static object? GetAny(IDataRecord record, string name)
    {
        var ordinal = record.GetOrdinal(name);
        return record.IsDBNull(ordinal) ? null : record.GetValue(ordinal);
    }

    private static Guid GetGuid(IDataRecord record, string name) => record.GetGuid(record.GetOrdinal(name));
    private static Guid? GetNullableGuid(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetGuid(record.GetOrdinal(name));
    private static string GetString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? string.Empty : record.GetString(record.GetOrdinal(name));
    private static string? GetNullableString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetString(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
    private static DateTime? GetNullableDate(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetDateTime(record.GetOrdinal(name));
}
