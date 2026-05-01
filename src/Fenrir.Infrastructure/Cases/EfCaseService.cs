using System.Data;
using System.Data.Common;
using Fenrir.Application.Abstractions;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Infrastructure.Cases;

public sealed class EfCaseService(FenrirDbContext dbContext) : ICaseService
{
    public async Task<CaseDto> CreateAsync(CaseCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var caseId = Guid.NewGuid();
        var caseNumber = $"CASE-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ExecuteAsync(connection, transaction, cancellationToken,
            """
            INSERT INTO "Cases" ("Id", "CaseNumber", "Title", "Description", "Severity", "Status", "AssignedTo", "CreatedBy", "CreatedAtUtc", "UpdatedAtUtc", "ClosedAtUtc", "Summary", "Conclusion")
            VALUES (@Id, @CaseNumber, @Title, @Description, @Severity, @Status, @AssignedTo, @CreatedBy, @CreatedAtUtc, @UpdatedAtUtc, NULL, NULL, NULL)
            """,
            ("Id", caseId),
            ("CaseNumber", caseNumber),
            ("Title", NormaliseOrDefault(request.Title, "Untitled investigation")),
            ("Description", request.Description),
            ("Severity", NormaliseOrDefault(request.Severity, "Medium")),
            ("Status", "New"),
            ("AssignedTo", request.AssignedTo),
            ("CreatedBy", NormaliseOrDefault(request.CreatedBy, "analyst")),
            ("CreatedAtUtc", now),
            ("UpdatedAtUtc", now));

        await InsertTimelineItemAsync(connection, transaction, caseId, now, "case_created", "Case created", request.Description, null, null, now, cancellationToken);

        if (request.EventId.HasValue)
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                """
                INSERT INTO "CaseEventLinks" ("Id", "CaseId", "EventId", "Reason", "CreatedAtUtc")
                VALUES (@Id, @CaseId, @EventId, @Reason, @CreatedAtUtc)
                """,
                ("Id", Guid.NewGuid()),
                ("CaseId", caseId),
                ("EventId", request.EventId.Value),
                ("Reason", "Initial event used to create case."),
                ("CreatedAtUtc", now));

            await InsertTimelineItemAsync(connection, transaction, caseId, now, "event_linked", "Initial SIEM event linked", null, request.EventId.Value, "SecurityEvent", now, cancellationToken);
        }

        if (request.IndicatorId.HasValue)
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                """
                INSERT INTO "CaseIndicatorLinks" ("Id", "CaseId", "IndicatorId", "Reason", "CreatedAtUtc")
                VALUES (@Id, @CaseId, @IndicatorId, @Reason, @CreatedAtUtc)
                """,
                ("Id", Guid.NewGuid()),
                ("CaseId", caseId),
                ("IndicatorId", request.IndicatorId.Value),
                ("Reason", "Initial IOC used to create case."),
                ("CreatedAtUtc", now));

            await InsertTimelineItemAsync(connection, transaction, caseId, now, "indicator_linked", "Initial IOC linked", null, request.IndicatorId.Value, "Indicator", now, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(caseId, cancellationToken) ?? throw new InvalidOperationException("Case was created but could not be reloaded.");
    }

    public async Task<IReadOnlyList<CaseSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var command = CreateCommand(connection, null,
            """
            SELECT c."Id", c."CaseNumber", c."Title", c."Severity", c."Status", c."AssignedTo", c."CreatedAtUtc", c."UpdatedAtUtc", c."ClosedAtUtc",
                   (SELECT COUNT(*) FROM "CaseEventLinks" cel WHERE cel."CaseId" = c."Id") AS "EventCount",
                   (SELECT COUNT(*) FROM "CaseIndicatorLinks" cil WHERE cil."CaseId" = c."Id") AS "IndicatorCount",
                   (SELECT COUNT(*) FROM "CaseNotes" cn WHERE cn."CaseId" = c."Id") AS "NoteCount",
                   (SELECT COUNT(*) FROM "CaseEvidence" ce WHERE ce."CaseId" = c."Id") AS "EvidenceCount"
            FROM "Cases" c
            ORDER BY c."UpdatedAtUtc" DESC
            LIMIT 500
            """);

        var results = new List<CaseSummaryDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new CaseSummaryDto(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                GetNullableString(reader, 5),
                reader.GetDateTime(6),
                reader.GetDateTime(7),
                GetNullableDateTime(reader, 8),
                reader.GetInt32(9),
                reader.GetInt32(10),
                reader.GetInt32(11),
                reader.GetInt32(12)));
        }

        return results;
    }

    public async Task<CaseDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        CaseHeader? header;
        await using (var command = CreateCommand(connection, null,
            """
            SELECT "Id", "CaseNumber", "Title", "Description", "Severity", "Status", "AssignedTo", "CreatedBy", "CreatedAtUtc", "UpdatedAtUtc", "ClosedAtUtc", "Summary", "Conclusion"
            FROM "Cases"
            WHERE "Id" = @Id
            """, ("Id", id)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            header = new CaseHeader(
                reader.GetGuid(0), reader.GetString(1), reader.GetString(2), GetNullableString(reader, 3), reader.GetString(4), reader.GetString(5),
                GetNullableString(reader, 6), GetNullableString(reader, 7), reader.GetDateTime(8), reader.GetDateTime(9), GetNullableDateTime(reader, 10),
                GetNullableString(reader, 11), GetNullableString(reader, 12));
        }

        return new CaseDto(
            header.Id,
            header.CaseNumber,
            header.Title,
            header.Description,
            header.Severity,
            header.Status,
            header.AssignedTo,
            header.CreatedBy,
            header.CreatedAtUtc,
            header.UpdatedAtUtc,
            header.ClosedAtUtc,
            header.Summary,
            header.Conclusion,
            await QueryNotesAsync(connection, id, cancellationToken),
            await QueryEvidenceAsync(connection, id, cancellationToken),
            await QueryEventLinksAsync(connection, id, cancellationToken),
            await QueryIndicatorLinksAsync(connection, id, cancellationToken),
            await QueryAssetLinksAsync(connection, id, cancellationToken),
            await QueryUserLinksAsync(connection, id, cancellationToken),
            await QueryTimelineAsync(connection, id, cancellationToken));
    }

    public async Task<CaseDto?> UpdateAsync(Guid id, CaseUpdateRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var status = NormaliseOrDefault(request.Status, existing.Status);
        DateTime? closedAt = IsClosedStatus(status) ? existing.ClosedAtUtc ?? DateTime.UtcNow : null;
        var now = DateTime.UtcNow;

        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await ExecuteAsync(connection, null, cancellationToken,
            """
            UPDATE "Cases"
            SET "Title" = @Title,
                "Description" = @Description,
                "Severity" = @Severity,
                "Status" = @Status,
                "AssignedTo" = @AssignedTo,
                "Summary" = @Summary,
                "Conclusion" = @Conclusion,
                "ClosedAtUtc" = @ClosedAtUtc,
                "UpdatedAtUtc" = @UpdatedAtUtc
            WHERE "Id" = @Id
            """,
            ("Id", id),
            ("Title", NormaliseOrDefault(request.Title, existing.Title)),
            ("Description", request.Description ?? existing.Description),
            ("Severity", NormaliseOrDefault(request.Severity, existing.Severity)),
            ("Status", status),
            ("AssignedTo", request.AssignedTo ?? existing.AssignedTo),
            ("Summary", request.Summary ?? existing.Summary),
            ("Conclusion", request.Conclusion ?? existing.Conclusion),
            ("ClosedAtUtc", closedAt),
            ("UpdatedAtUtc", now));

        return await GetAsync(id, cancellationToken);
    }

    public async Task<CaseDto?> AddNoteAsync(Guid id, CaseNoteCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                "INSERT INTO \"CaseNotes\" (\"Id\", \"CaseId\", \"Author\", \"Note\", \"CreatedAtUtc\") VALUES (@Id, @CaseId, @Author, @Note, @CreatedAtUtc)",
                ("Id", Guid.NewGuid()), ("CaseId", id), ("Author", NormaliseOrDefault(request.Author, "analyst")), ("Note", NormaliseOrDefault(request.Note, string.Empty)), ("CreatedAtUtc", now));
            await InsertTimelineItemAsync(connection, transaction, id, now, "note_added", "Analyst note added", request.Note, null, null, now, cancellationToken);
        });
    }

    public async Task<CaseDto?> AddEvidenceAsync(Guid id, CaseEvidenceCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                """
                INSERT INTO "CaseEvidence" ("Id", "CaseId", "EvidenceType", "FileName", "ContentType", "StorageReference", "Sha256", "UploadedBy", "CreatedAtUtc")
                VALUES (@Id, @CaseId, @EvidenceType, @FileName, @ContentType, @StorageReference, @Sha256, @UploadedBy, @CreatedAtUtc)
                """,
                ("Id", Guid.NewGuid()), ("CaseId", id), ("EvidenceType", NormaliseOrDefault(request.EvidenceType, "artifact")), ("FileName", NormaliseOrDefault(request.FileName, "evidence")),
                ("ContentType", request.ContentType), ("StorageReference", NormaliseOrDefault(request.StorageReference, string.Empty)), ("Sha256", request.Sha256), ("UploadedBy", NormaliseOrDefault(request.UploadedBy, "analyst")), ("CreatedAtUtc", now));
            await InsertTimelineItemAsync(connection, transaction, id, now, "evidence_added", "Evidence added", request.FileName, null, null, now, cancellationToken);
        });
    }

    public async Task<CaseDto?> LinkEventAsync(Guid id, CaseEventLinkRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                "INSERT INTO \"CaseEventLinks\" (\"Id\", \"CaseId\", \"EventId\", \"Reason\", \"CreatedAtUtc\") VALUES (@Id, @CaseId, @EventId, @Reason, @CreatedAtUtc)",
                ("Id", Guid.NewGuid()), ("CaseId", id), ("EventId", request.EventId), ("Reason", request.Reason), ("CreatedAtUtc", now));
            await InsertTimelineItemAsync(connection, transaction, id, now, "event_linked", "SIEM event linked", request.Reason, request.EventId, "SecurityEvent", now, cancellationToken);
        });
    }

    public async Task<CaseDto?> LinkIndicatorAsync(Guid id, CaseIndicatorLinkRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                "INSERT INTO \"CaseIndicatorLinks\" (\"Id\", \"CaseId\", \"IndicatorId\", \"Reason\", \"CreatedAtUtc\") VALUES (@Id, @CaseId, @IndicatorId, @Reason, @CreatedAtUtc)",
                ("Id", Guid.NewGuid()), ("CaseId", id), ("IndicatorId", request.IndicatorId), ("Reason", request.Reason), ("CreatedAtUtc", now));
            await InsertTimelineItemAsync(connection, transaction, id, now, "indicator_linked", "IOC linked", request.Reason, request.IndicatorId, "Indicator", now, cancellationToken);
        });
    }

    public async Task<CaseDto?> LinkAssetAsync(Guid id, CaseAssetLinkRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                "INSERT INTO \"CaseAssetLinks\" (\"Id\", \"CaseId\", \"AssetReference\", \"Reason\", \"CreatedAtUtc\") VALUES (@Id, @CaseId, @AssetReference, @Reason, @CreatedAtUtc)",
                ("Id", Guid.NewGuid()), ("CaseId", id), ("AssetReference", NormaliseOrDefault(request.AssetReference, string.Empty)), ("Reason", request.Reason), ("CreatedAtUtc", now));
            await InsertTimelineItemAsync(connection, transaction, id, now, "asset_linked", "Asset linked", request.AssetReference, null, null, now, cancellationToken);
        });
    }

    public async Task<CaseDto?> LinkUserAsync(Guid id, CaseUserLinkRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await ExecuteAsync(connection, transaction, cancellationToken,
                "INSERT INTO \"CaseUserLinks\" (\"Id\", \"CaseId\", \"UserReference\", \"Reason\", \"CreatedAtUtc\") VALUES (@Id, @CaseId, @UserReference, @Reason, @CreatedAtUtc)",
                ("Id", Guid.NewGuid()), ("CaseId", id), ("UserReference", NormaliseOrDefault(request.UserReference, string.Empty)), ("Reason", request.Reason), ("CreatedAtUtc", now));
            await InsertTimelineItemAsync(connection, transaction, id, now, "user_linked", "User linked", request.UserReference, null, null, now, cancellationToken);
        });
    }

    public async Task<CaseDto?> AddTimelineItemAsync(Guid id, CaseTimelineItemCreateRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await AddChildAsync(id, cancellationToken, async (connection, transaction) =>
        {
            await InsertTimelineItemAsync(connection, transaction, id, request.OccurredAtUtc?.ToUniversalTime() ?? now, NormaliseOrDefault(request.ItemType, "manual"), NormaliseOrDefault(request.Title, "Timeline item"), request.Description, request.RelatedEntityId, request.RelatedEntityType, now, cancellationToken);
        });
    }

    private async Task<CaseDto?> AddChildAsync(Guid caseId, CancellationToken cancellationToken, Func<DbConnection, DbTransaction, Task> action)
    {
        if (await GetAsync(caseId, cancellationToken) is null)
        {
            return null;
        }

        var connection = dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await action(connection, transaction);
        await ExecuteAsync(connection, transaction, cancellationToken, "UPDATE \"Cases\" SET \"UpdatedAtUtc\" = @UpdatedAtUtc WHERE \"Id\" = @Id", ("Id", caseId), ("UpdatedAtUtc", DateTime.UtcNow));
        await transaction.CommitAsync(cancellationToken);
        return await GetAsync(caseId, cancellationToken);
    }

    private static async Task InsertTimelineItemAsync(DbConnection connection, DbTransaction? transaction, Guid caseId, DateTime occurredAtUtc, string itemType, string title, string? description, Guid? relatedEntityId, string? relatedEntityType, DateTime createdAtUtc, CancellationToken cancellationToken)
    {
        await ExecuteAsync(connection, transaction, cancellationToken,
            """
            INSERT INTO "CaseTimelineItems" ("Id", "CaseId", "OccurredAtUtc", "ItemType", "Title", "Description", "RelatedEntityId", "RelatedEntityType", "CreatedAtUtc")
            VALUES (@Id, @CaseId, @OccurredAtUtc, @ItemType, @Title, @Description, @RelatedEntityId, @RelatedEntityType, @CreatedAtUtc)
            """,
            ("Id", Guid.NewGuid()), ("CaseId", caseId), ("OccurredAtUtc", occurredAtUtc), ("ItemType", itemType), ("Title", title), ("Description", description),
            ("RelatedEntityId", relatedEntityId), ("RelatedEntityType", relatedEntityType), ("CreatedAtUtc", createdAtUtc));
    }

    private static async Task<IReadOnlyList<CaseNoteDto>> QueryNotesAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseNoteDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"Author\", \"Note\", \"CreatedAtUtc\" FROM \"CaseNotes\" WHERE \"CaseId\" = @CaseId ORDER BY \"CreatedAtUtc\" DESC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), reader.GetDateTime(4)));
        return results;
    }

    private static async Task<IReadOnlyList<CaseEvidenceDto>> QueryEvidenceAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseEvidenceDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"EvidenceType\", \"FileName\", \"ContentType\", \"StorageReference\", \"Sha256\", \"UploadedBy\", \"CreatedAtUtc\" FROM \"CaseEvidence\" WHERE \"CaseId\" = @CaseId ORDER BY \"CreatedAtUtc\" DESC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3), GetNullableString(reader, 4), reader.GetString(5), GetNullableString(reader, 6), reader.GetString(7), reader.GetDateTime(8)));
        return results;
    }

    private static async Task<IReadOnlyList<CaseEventLinkDto>> QueryEventLinksAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseEventLinkDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"EventId\", \"Reason\", \"CreatedAtUtc\" FROM \"CaseEventLinks\" WHERE \"CaseId\" = @CaseId ORDER BY \"CreatedAtUtc\" DESC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), GetNullableString(reader, 3), reader.GetDateTime(4)));
        return results;
    }

    private static async Task<IReadOnlyList<CaseIndicatorLinkDto>> QueryIndicatorLinksAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseIndicatorLinkDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"IndicatorId\", \"Reason\", \"CreatedAtUtc\" FROM \"CaseIndicatorLinks\" WHERE \"CaseId\" = @CaseId ORDER BY \"CreatedAtUtc\" DESC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), GetNullableString(reader, 3), reader.GetDateTime(4)));
        return results;
    }

    private static async Task<IReadOnlyList<CaseAssetLinkDto>> QueryAssetLinksAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseAssetLinkDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"AssetReference\", \"Reason\", \"CreatedAtUtc\" FROM \"CaseAssetLinks\" WHERE \"CaseId\" = @CaseId ORDER BY \"CreatedAtUtc\" DESC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), GetNullableString(reader, 3), reader.GetDateTime(4)));
        return results;
    }

    private static async Task<IReadOnlyList<CaseUserLinkDto>> QueryUserLinksAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseUserLinkDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"UserReference\", \"Reason\", \"CreatedAtUtc\" FROM \"CaseUserLinks\" WHERE \"CaseId\" = @CaseId ORDER BY \"CreatedAtUtc\" DESC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), GetNullableString(reader, 3), reader.GetDateTime(4)));
        return results;
    }

    private static async Task<IReadOnlyList<CaseTimelineItemDto>> QueryTimelineAsync(DbConnection connection, Guid caseId, CancellationToken cancellationToken)
    {
        var results = new List<CaseTimelineItemDto>();
        await using var command = CreateCommand(connection, null, "SELECT \"Id\", \"CaseId\", \"OccurredAtUtc\", \"ItemType\", \"Title\", \"Description\", \"RelatedEntityId\", \"RelatedEntityType\", \"CreatedAtUtc\" FROM \"CaseTimelineItems\" WHERE \"CaseId\" = @CaseId ORDER BY \"OccurredAtUtc\" ASC", ("CaseId", caseId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) results.Add(new(reader.GetGuid(0), reader.GetGuid(1), reader.GetDateTime(2), reader.GetString(3), reader.GetString(4), GetNullableString(reader, 5), GetNullableGuid(reader, 6), GetNullableString(reader, 7), reader.GetDateTime(8)));
        return results;
    }

    private static async Task EnsureOpenAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }
    }

    private static DbCommand CreateCommand(DbConnection connection, DbTransaction? transaction, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        return command;
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken, string sql, params (string Name, object? Value)[] parameters)
    {
        await using var command = CreateCommand(connection, transaction, sql, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string? GetNullableString(DbDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static DateTime? GetNullableDateTime(DbDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static Guid? GetNullableGuid(DbDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetGuid(index);
    private static string NormaliseOrDefault(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    private static bool IsClosedStatus(string status) => status.StartsWith("Closed", StringComparison.OrdinalIgnoreCase);

    private sealed record CaseHeader(Guid Id, string CaseNumber, string Title, string? Description, string Severity, string Status, string? AssignedTo, string? CreatedBy, DateTime CreatedAtUtc, DateTime UpdatedAtUtc, DateTime? ClosedAtUtc, string? Summary, string? Conclusion);
}
