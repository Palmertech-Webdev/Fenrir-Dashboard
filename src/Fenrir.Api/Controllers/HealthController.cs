using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController(FenrirDbContext dbContext, ILogger<HealthController> logger) : ControllerBase
{
    private static readonly string[] RequiredTables =
    [
        "SiemEvents",
        "SiemLogSources",
        "SiemIngestionJobs"
    ];

    [HttpGet("database")]
    public async Task<ActionResult<DatabaseHealthResponse>> Database(CancellationToken cancellationToken)
    {
        var provider = dbContext.Database.ProviderName ?? "unknown";
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new DatabaseHealthResponse(
                Status: "Unhealthy",
                Provider: provider,
                CanConnect: false,
                MissingTables: RequiredTables,
                AppliedMigrations: [],
                PendingMigrations: [],
                Message: "Database connection failed. Check the FenrirDb connection string and PostgreSQL service."));
        }

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        var missingTables = await GetMissingTablesAsync(provider, cancellationToken);

        var status = missingTables.Count == 0 && !pendingMigrations.Any()
            ? "Healthy"
            : "Degraded";

        var message = status == "Healthy"
            ? "Database is reachable, required SIEM tables exist, and no EF migrations are pending."
            : "Database is reachable, but the schema is not fully aligned with the application. Run dotnet ef database update.";

        var response = new DatabaseHealthResponse(
            Status: status,
            Provider: provider,
            CanConnect: true,
            MissingTables: missingTables,
            AppliedMigrations: appliedMigrations.ToArray(),
            PendingMigrations: pendingMigrations.ToArray(),
            Message: message);

        return status == "Healthy" ? Ok(response) : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    private async Task<IReadOnlyList<string>> GetMissingTablesAsync(string provider, CancellationToken cancellationToken)
    {
        try
        {
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                return await GetMissingPostgresTablesAsync(cancellationToken);
            }

            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                return await GetMissingSqliteTablesAsync(cancellationToken);
            }

            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to inspect database schema health.");
            return RequiredTables;
        }
    }

    private async Task<IReadOnlyList<string>> GetMissingPostgresTablesAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        await using var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        foreach (var table in RequiredTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "select exists (select 1 from information_schema.tables where table_schema = 'public' and table_name = @table_name);";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "table_name";
            parameter.Value = table;
            command.Parameters.Add(parameter);

            var exists = (bool?)await command.ExecuteScalarAsync(cancellationToken) ?? false;
            if (!exists)
            {
                missing.Add(table);
            }
        }

        return missing;
    }

    private async Task<IReadOnlyList<string>> GetMissingSqliteTablesAsync(CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        await using var connection = dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        foreach (var table in RequiredTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "select count(*) from sqlite_master where type = 'table' and name = @table_name;";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "table_name";
            parameter.Value = table;
            command.Parameters.Add(parameter);

            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            if (count == 0)
            {
                missing.Add(table);
            }
        }

        return missing;
    }
}

public sealed record DatabaseHealthResponse(
    string Status,
    string Provider,
    bool CanConnect,
    IReadOnlyList<string> MissingTables,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> PendingMigrations,
    string Message);
