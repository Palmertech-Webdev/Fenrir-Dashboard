using System.Data;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/workspace")]
public sealed class WorkspaceController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet("mode")]
    public async Task<ActionResult<WorkspaceModeDto>> GetMode([FromQuery] string userKey = "local", CancellationToken cancellationToken = default)
    {
        await EnsureModeAsync(userKey, cancellationToken);
        var mode = await ReadModeAsync(userKey, cancellationToken);
        return mode is null ? NotFound() : Ok(mode);
    }

    [HttpPut("mode")]
    public async Task<ActionResult<WorkspaceModeDto>> UpdateMode(WorkspaceModeUpdateRequest request, CancellationToken cancellationToken)
    {
        var preset = GetPreset(request.Mode);
        var userKey = string.IsNullOrWhiteSpace(request.UserKey) ? "local" : request.UserKey.Trim();
        await EnsureModeAsync(userKey, cancellationToken);

        await ExecuteAsync("""
            UPDATE "WorkspaceModes"
            SET "Mode" = @Mode,
                "Role" = @Role,
                "DisplayName" = @DisplayName,
                "Description" = @Description,
                "ShowAdvancedFeatures" = @ShowAdvancedFeatures,
                "AllowResponseActions" = @AllowResponseActions,
                "AllowEvidenceExports" = @AllowEvidenceExports,
                "AllowSourceConfiguration" = @AllowSourceConfiguration,
                "UpdatedAtUtc" = @UpdatedAtUtc
            WHERE "UserKey" = @UserKey
            """, cancellationToken,
            ("UserKey", userKey),
            ("Mode", preset.Mode),
            ("Role", string.IsNullOrWhiteSpace(request.Role) ? preset.Role : request.Role),
            ("DisplayName", preset.DisplayName),
            ("Description", preset.Description),
            ("ShowAdvancedFeatures", request.ShowAdvancedFeatures ?? preset.ShowAdvancedFeatures),
            ("AllowResponseActions", request.AllowResponseActions ?? preset.AllowResponseActions),
            ("AllowEvidenceExports", request.AllowEvidenceExports ?? preset.AllowEvidenceExports),
            ("AllowSourceConfiguration", request.AllowSourceConfiguration ?? preset.AllowSourceConfiguration),
            ("UpdatedAtUtc", DateTime.UtcNow));

        var mode = await ReadModeAsync(userKey, cancellationToken);
        return mode is null ? NotFound() : Ok(mode);
    }

    [HttpGet("mode/presets")]
    public ActionResult<IReadOnlyList<WorkspaceModePresetDto>> GetPresets()
    {
        return Ok(new[] { AnalystPreset(), HomeUserPreset() });
    }

    [HttpGet("features")]
    public ActionResult<IReadOnlyList<FeatureAccessDto>> GetFeatures()
    {
        return Ok(new[]
        {
            new FeatureAccessDto("dashboard", "Dashboard", "Core", true, true, false, "Both modes need a security overview."),
            new FeatureAccessDto("email", "Email Tools", "Investigation", true, true, false, "Header and sender validation is useful for analysts and home users."),
            new FeatureAccessDto("ioc", "IOC Checking", "Investigation", true, true, false, "IOC checks can remain available with simplified explanations."),
            new FeatureAccessDto("dns", "DNS Monitoring", "Validation", true, true, false, "Domain posture checks are safe and useful in both modes."),
            new FeatureAccessDto("darkweb", "Dark Web", "Validation", true, true, false, "Exposure checks can be shown in simplified wording."),
            new FeatureAccessDto("network", "Network Scans", "Technical", true, false, true, "Network scanning can be noisy and should be analyst-oriented."),
            new FeatureAccessDto("siem", "SIEM Collector", "Advanced", true, false, true, "Telemetry ingestion, parsers and sources are analyst workflows."),
            new FeatureAccessDto("correlation", "Correlation", "Advanced", true, false, true, "Correlation rules are SOC maturity workflows."),
            new FeatureAccessDto("response", "Response Playbooks", "Response", true, false, true, "Response actions should not be exposed in home mode."),
            new FeatureAccessDto("hunts", "Hunts / DFIR", "DFIR", true, false, true, "Hunts and collection workflows require analyst context."),
            new FeatureAccessDto("reports", "Reports / Integrity", "Evidence", true, false, true, "Formal evidence exports are analyst workflows."),
            new FeatureAccessDto("findings", "Findings", "Core", true, true, false, "Both modes should see findings."),
            new FeatureAccessDto("jobs", "Jobs", "Operations", true, false, true, "Operational job details are advanced."),
        });
    }

    private async Task EnsureModeAsync(string userKey, CancellationToken cancellationToken)
    {
        var count = Convert.ToInt32(await ScalarAsync("SELECT COUNT(*) FROM \"WorkspaceModes\" WHERE \"UserKey\" = @UserKey", cancellationToken, ("UserKey", userKey)));
        if (count > 0) return;
        var preset = AnalystPreset();
        await ExecuteAsync("""
            INSERT INTO "WorkspaceModes" ("Id", "UserKey", "Mode", "Role", "DisplayName", "Description", "ShowAdvancedFeatures", "AllowResponseActions", "AllowEvidenceExports", "AllowSourceConfiguration", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @UserKey, @Mode, @Role, @DisplayName, @Description, @ShowAdvancedFeatures, @AllowResponseActions, @AllowEvidenceExports, @AllowSourceConfiguration, @CreatedAtUtc, @UpdatedAtUtc)
            """, cancellationToken,
            ("Id", Guid.NewGuid()),
            ("UserKey", userKey),
            ("Mode", preset.Mode),
            ("Role", preset.Role),
            ("DisplayName", preset.DisplayName),
            ("Description", preset.Description),
            ("ShowAdvancedFeatures", preset.ShowAdvancedFeatures),
            ("AllowResponseActions", preset.AllowResponseActions),
            ("AllowEvidenceExports", preset.AllowEvidenceExports),
            ("AllowSourceConfiguration", preset.AllowSourceConfiguration),
            ("CreatedAtUtc", DateTime.UtcNow),
            ("UpdatedAtUtc", DateTime.UtcNow));
    }

    private async Task<WorkspaceModeDto?> ReadModeAsync(string userKey, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync("SELECT * FROM \"WorkspaceModes\" WHERE \"UserKey\" = @UserKey LIMIT 1", cancellationToken, ("UserKey", userKey));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new WorkspaceModeDto(
            GetGuid(reader, "Id"),
            GetString(reader, "UserKey"),
            GetString(reader, "Mode"),
            GetString(reader, "Role"),
            GetString(reader, "DisplayName"),
            GetString(reader, "Description"),
            GetBool(reader, "ShowAdvancedFeatures"),
            GetBool(reader, "AllowResponseActions"),
            GetBool(reader, "AllowEvidenceExports"),
            GetBool(reader, "AllowSourceConfiguration"),
            GetDate(reader, "UpdatedAtUtc"));
    }

    private static WorkspaceModePresetDto GetPreset(string mode)
    {
        return mode.Equals("HomeUser", StringComparison.OrdinalIgnoreCase) || mode.Equals("Home User", StringComparison.OrdinalIgnoreCase)
            ? HomeUserPreset()
            : AnalystPreset();
    }

    private static WorkspaceModePresetDto AnalystPreset() => new(
        "Analyst",
        "Analyst",
        "Analyst Mode",
        "Full SOC investigation workspace with SIEM, source configuration, hunts, response playbooks, reports and evidence integrity.",
        true,
        true,
        true,
        true,
        ["dashboard", "email", "ioc", "dns", "darkweb", "network", "siem", "correlation", "response", "hunts", "reports", "findings", "jobs"],
        []);

    private static WorkspaceModePresetDto HomeUserPreset() => new(
        "HomeUser",
        "HomeUser",
        "Home User Mode",
        "Simplified validation workspace focused on email, IOC, DNS, dark web exposure and understandable findings.",
        false,
        false,
        false,
        false,
        ["dashboard", "email", "ioc", "dns", "darkweb", "findings"],
        ["network", "siem", "correlation", "response", "hunts", "reports", "jobs"]);

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<object?> ScalarAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters);
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
    private static string GetString(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? string.Empty : record.GetString(record.GetOrdinal(name));
    private static bool GetBool(IDataRecord record, string name) => record.GetBoolean(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
}
