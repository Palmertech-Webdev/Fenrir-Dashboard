using System.Data;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/roadmap")]
public sealed class RoadmapController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet("readiness")]
    public ActionResult<PhaseReadinessSummaryDto> GetReadiness()
    {
        var phases = BuildPhaseReadiness();
        return Ok(new PhaseReadinessSummaryDto(
            DateTime.UtcNow,
            phases.Count,
            phases.Count(p => p.Status == "Implemented"),
            phases.Count(p => p.Status == "NeedsHardening"),
            phases));
    }

    [HttpGet("improvements")]
    public async Task<ActionResult<IReadOnlyList<ImprovementBacklogItemDto>>> ListImprovements(CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);
        var items = new List<ImprovementBacklogItemDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"ImprovementBacklogItems\" ORDER BY \"CreatedAtUtc\" DESC LIMIT 250", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new ImprovementBacklogItemDto(
                GetGuid(reader, "Id"),
                GetString(reader, "Title"),
                GetString(reader, "Area"),
                GetString(reader, "Priority"),
                GetString(reader, "Description"),
                GetString(reader, "Status"),
                GetDate(reader, "CreatedAtUtc")));
        }
        return Ok(items);
    }

    [HttpPost("improvements")]
    public async Task<ActionResult<ImprovementBacklogItemDto>> CreateImprovement(CreateImprovementBacklogItemRequest request, CancellationToken cancellationToken)
    {
        await EnsureTableAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Improvement title is required.");

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await ExecuteAsync("""
            INSERT INTO "ImprovementBacklogItems" ("Id", "Title", "Area", "Priority", "Description", "Status", "CreatedAtUtc")
            VALUES (@Id, @Title, @Area, @Priority, @Description, 'New', @CreatedAtUtc)
            """, cancellationToken,
            ("Id", id),
            ("Title", request.Title.Trim()),
            ("Area", string.IsNullOrWhiteSpace(request.Area) ? "General" : request.Area.Trim()),
            ("Priority", NormalisePriority(request.Priority)),
            ("Description", request.Description?.Trim() ?? string.Empty),
            ("CreatedAtUtc", now));

        return Created($"/api/roadmap/improvements/{id}", new ImprovementBacklogItemDto(id, request.Title.Trim(), string.IsNullOrWhiteSpace(request.Area) ? "General" : request.Area.Trim(), NormalisePriority(request.Priority), request.Description?.Trim() ?? string.Empty, "New", now));
    }

    private static IReadOnlyList<PhaseReadinessDto> BuildPhaseReadiness()
    {
        return new[]
        {
            Phase(0, "Phase 0", "Stabilise database, migrations and API errors", "Implemented", "Baseline SIEM tables, EF tooling fixes and runtime stability work completed.", "Core dashboard/API stability", ["/api/siem/events", "/api/siem/sources", "/api/siem/ingestion-jobs"], ["SIEM endpoints no longer rely on missing tables.", "Database update workflow established."], ["Add automated migration smoke tests."]),
            Phase(1, "Phase 1", "Source configuration model", "Implemented", "Expanded source model with config, secret references, state and health surfaces.", "SIEM Collector / Sources", ["/api/siem/sources"], ["Create/edit source workflows.", "Secret references shown without exposing secret values."], ["Replace remaining raw SQL with repository abstractions where practical."]),
            Phase(2, "Phase 2", "Parser pack architecture", "Implemented", "Parser registry and normalised parser output model introduced.", "SIEM Collector / Parser assignment", ["/api/siem/ingest/batch"], ["Generic parser and named parser profiles available.", "Parsed fields map into normalised SIEM events."], ["Add parser unit fixture coverage for each sample source."]),
            Phase(3, "Phase 3", "Parsed-field search and pivots", "Implemented", "Structured SIEM search supports pivots across IP, user, host, domain, hash, category and severity.", "SIEM Collector / Telemetry Search", ["/api/siem/events", "/api/siem/events/search"], ["Dashboard search form uses parsed-field filters.", "Event detail exposes parsed fields and raw JSON."], ["Add saved searches and query history."]),
            Phase(4, "Phase 4", "Agent enrolment, heartbeat and truthfulness", "Implemented", "Agent status model and last-check-in logic added for truthful source/agent visibility.", "Agents / SIEM source health", ["/api/agents", "/api/agents/enrol", "/api/agents/{agentId}/heartbeat"], ["Online/offline state available.", "Last heartbeat and telemetry tracking available."], ["Add signed agent enrolment challenge-response."]),
            Phase(5, "Phase 5", "IOC enrichment with MISP/TAXII", "Implemented", "Threat intelligence source and indicator enrichment plane added.", "IOC Checking / SIEM enrichment", ["/api/threat-intel", "/api/ioc"], ["IOC matching and enrichment fields surfaced.", "Threat intel source model available."], ["Add real TAXII collection polling and MISP connector hardening."]),
            Phase(6, "Phase 6", "Source health dashboards", "Implemented", "Source health metrics, recent jobs and status dashboards added.", "SIEM Collector / Source Health", ["/api/siem/sources", "/api/siem/ingestion-jobs"], ["Latest job and health state visible.", "Parse success/failure counters shown."], ["Add time-series health snapshots and charts."]),
            Phase(7, "Phase 7", "Durable ingestion queue and parser workers", "Implemented", "Queue-backed ingestion job model and worker path introduced for async parsing.", "Ingestion Jobs", ["/api/siem/ingest/batch", "/api/siem/ingestion-jobs"], ["Ingestion jobs show queued/processing/completed state.", "Worker hosted service added."], ["Move from MVP job table to Hangfire/PostgreSQL or RabbitMQ for production scale."]),
            Phase(8, "Phase 8", "Case and incident model", "Implemented", "Cases, notes, evidence and event/IOC linking added to investigation workflow.", "Cases / Findings / Event pivots", ["/api/cases"], ["Create case from suspicious activity.", "Attach notes and evidence."], ["Add report export templates per case type."]),
            Phase(9, "Phase 9", "Dedicated investigation views", "Implemented", "Email, cloud and Windows investigation pivots connected to the dashboard.", "Investigation Views", ["/api/investigations"], ["Dedicated pivots for user/IP/host/mailbox/cloud action.", "Views answer who/what/from where/against what."], ["Add guided analyst question flow per investigation type."]),
            Phase(10, "Phase 10", "Correlation and incident stitching", "Implemented", "Correlation rules and entity-style incident stitching started.", "Correlation", ["/api/correlation"], ["Rules and correlated outputs surfaced in dashboard.", "Cross-event incident narrative groundwork added."], ["Add rule simulation and MITRE coverage scoring."]),
            Phase(11, "Phase 11", "Response integrations and playbooks", "Implemented", "Response playbook model and dashboard controls added.", "Response Playbooks", ["/api/response-playbooks"], ["Playbooks list and run-preparation surfaces available.", "Manual response workflow controls present."], ["Add approval gates and connector-specific response adapters."]),
            Phase(12, "Phase 12", "Hunt packs and DFIR collection", "Implemented", "Hunt/DFIR collection workflows inspired by Velociraptor/Wazuh added.", "Hunts / DFIR", ["/api/hunts"], ["Hunt packs and collection jobs visible.", "Evidence-oriented collection workflow started."], ["Add endpoint-side collectors and signed artefact bundles."]),
            Phase(13, "Phase 13", "Reports and evidence integrity", "Implemented", "Investigation reports and evidence hash verification added.", "Reports / Integrity", ["/api/reports", "/api/reports/evidence-integrity"], ["Markdown reports generated.", "Evidence seal/verify workflow available."], ["Add PDF export and report signing certificate chain."]),
            Phase(14, "Phase 14", "Role-based analyst and home user modes", "Implemented", "Workspace modes added for analyst and simplified home-user views.", "Workspace Mode", ["/api/workspace/mode", "/api/workspace/features"], ["Mode switcher available.", "Home mode hides advanced SOC/DFIR workflows."], ["Enforce backend authorisation policies, not only UI hiding."]),
            Phase(15, "Phase 15", "Signed update and rule distribution", "Implemented", "Update channels, signed package metadata, manifests and publish/revoke workflow added.", "Signed Updates", ["/api/updates/channels", "/api/updates/packages", "/api/updates/manifest/{channelName}"], ["Stable/preview channels available.", "Package verification and manifest preview available."], ["Implement real asymmetric signature verification and agent-side trust pinning."])
        };
    }

    private static PhaseReadinessDto Phase(int order, string phase, string title, string status, string summary, string dashboardSurface, IReadOnlyList<string> apiSurfaces, IReadOnlyList<string> evidence, IReadOnlyList<string> hardening)
        => new(order, phase, title, status, summary, dashboardSurface, apiSurfaces, evidence, hardening);

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS "ImprovementBacklogItems" (
                "Id" uuid PRIMARY KEY,
                "Title" character varying(220) NOT NULL,
                "Area" character varying(120) NOT NULL,
                "Priority" character varying(40) NOT NULL,
                "Description" text NOT NULL,
                "Status" character varying(60) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL
            )
            """, cancellationToken);
    }

    private static string NormalisePriority(string value) => value.Equals("Critical", StringComparison.OrdinalIgnoreCase) ? "Critical" : value.Equals("High", StringComparison.OrdinalIgnoreCase) ? "High" : value.Equals("Low", StringComparison.OrdinalIgnoreCase) ? "Low" : "Medium";

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

    private static Guid GetGuid(IDataRecord record, string name) => record.GetGuid(record.GetOrdinal(name));
    private static string GetString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? string.Empty : record.GetString(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
}
