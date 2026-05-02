using System.Data;
using Fenrir.Contracts;
using Fenrir.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Api.Controllers;

[ApiController]
[Route("api/updates")]
public sealed class UpdatesController(FenrirDbContext dbContext) : ControllerBase
{
    [HttpGet("channels")]
    public async Task<ActionResult<IReadOnlyList<UpdateChannelDto>>> ListChannels(CancellationToken cancellationToken)
    {
        await EnsureSeedChannelsAsync(cancellationToken);
        return Ok(await ReadChannelsAsync(cancellationToken));
    }

    [HttpPost("channels")]
    public async Task<ActionResult<UpdateChannelDto>> CreateChannel(CreateUpdateChannelRequest request, CancellationToken cancellationToken)
    {
        await EnsureSeedChannelsAsync(cancellationToken);
        var name = NormaliseChannelName(request.Name);
        var existing = (await ReadChannelsAsync(cancellationToken)).FirstOrDefault(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return Ok(existing);

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        await ExecuteAsync("""
            INSERT INTO "UpdateChannels" ("Id", "Name", "Description", "IsEnabled", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @Name, @Description, @IsEnabled, @CreatedAtUtc, @UpdatedAtUtc)
            """, cancellationToken,
            ("Id", id), ("Name", name), ("Description", request.Description ?? string.Empty), ("IsEnabled", request.IsEnabled), ("CreatedAtUtc", now), ("UpdatedAtUtc", now));

        var created = (await ReadChannelsAsync(cancellationToken)).First(item => item.Id == id);
        return Created($"/api/updates/channels/{created.Name}", created);
    }

    [HttpGet("packages")]
    public async Task<ActionResult<IReadOnlyList<UpdatePackageDto>>> ListPackages([FromQuery] string? channelName, CancellationToken cancellationToken)
    {
        await EnsureSeedChannelsAsync(cancellationToken);
        return Ok(await ReadPackagesAsync(channelName, cancellationToken));
    }

    [HttpPost("packages")]
    public async Task<ActionResult<UpdatePackageDto>> CreatePackage(CreateUpdatePackageRequest request, CancellationToken cancellationToken)
    {
        await EnsureSeedChannelsAsync(cancellationToken);
        var channel = (await ReadChannelsAsync(cancellationToken)).FirstOrDefault(item => item.Name.Equals(NormaliseChannelName(request.ChannelName), StringComparison.OrdinalIgnoreCase));
        if (channel is null) return BadRequest("Update channel does not exist.");

        var validation = ValidatePackage(request.DownloadUrl, request.Sha256, request.SignatureAlgorithm, request.Signature, request.PublicKeyId);
        if (!validation.IsValid) return BadRequest(validation);

        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var status = NormaliseStatus(request.Status);
        await ExecuteAsync("""
            INSERT INTO "UpdatePackages" ("Id", "ChannelId", "PackageType", "Name", "Version", "MinimumAppVersion", "TargetPlatform", "DownloadUrl", "Sha256", "SizeBytes", "SignatureAlgorithm", "Signature", "PublicKeyId", "Status", "ReleaseNotes", "CreatedAtUtc", "PublishedAtUtc", "RevokedAtUtc")
            VALUES (@Id, @ChannelId, @PackageType, @Name, @Version, @MinimumAppVersion, @TargetPlatform, @DownloadUrl, @Sha256, @SizeBytes, @SignatureAlgorithm, @Signature, @PublicKeyId, @Status, @ReleaseNotes, @CreatedAtUtc, @PublishedAtUtc, @RevokedAtUtc)
            """, cancellationToken,
            ("Id", id), ("ChannelId", channel.Id), ("PackageType", request.PackageType), ("Name", request.Name), ("Version", request.Version),
            ("MinimumAppVersion", request.MinimumAppVersion), ("TargetPlatform", request.TargetPlatform), ("DownloadUrl", request.DownloadUrl),
            ("Sha256", request.Sha256.ToLowerInvariant()), ("SizeBytes", request.SizeBytes), ("SignatureAlgorithm", request.SignatureAlgorithm),
            ("Signature", request.Signature), ("PublicKeyId", request.PublicKeyId), ("Status", status), ("ReleaseNotes", request.ReleaseNotes ?? string.Empty),
            ("CreatedAtUtc", now), ("PublishedAtUtc", status == "Published" ? now : null), ("RevokedAtUtc", null));

        var created = (await ReadPackagesAsync(null, cancellationToken)).First(item => item.Id == id);
        return Created($"/api/updates/packages/{id}", created);
    }

    [HttpPost("packages/{id:guid}/publish")]
    public async Task<ActionResult<UpdatePackageDto>> PublishPackage(Guid id, CancellationToken cancellationToken)
    {
        await ExecuteAsync("UPDATE \"UpdatePackages\" SET \"Status\" = 'Published', \"PublishedAtUtc\" = @Now, \"RevokedAtUtc\" = NULL WHERE \"Id\" = @Id", cancellationToken, ("Id", id), ("Now", DateTime.UtcNow));
        var package = (await ReadPackagesAsync(null, cancellationToken)).FirstOrDefault(item => item.Id == id);
        return package is null ? NotFound() : Ok(package);
    }

    [HttpPost("packages/{id:guid}/revoke")]
    public async Task<ActionResult<UpdatePackageDto>> RevokePackage(Guid id, CancellationToken cancellationToken)
    {
        await ExecuteAsync("UPDATE \"UpdatePackages\" SET \"Status\" = 'Revoked', \"RevokedAtUtc\" = @Now WHERE \"Id\" = @Id", cancellationToken, ("Id", id), ("Now", DateTime.UtcNow));
        var package = (await ReadPackagesAsync(null, cancellationToken)).FirstOrDefault(item => item.Id == id);
        return package is null ? NotFound() : Ok(package);
    }

    [HttpGet("manifest/{channelName}")]
    public async Task<ActionResult<UpdateManifestDto>> GetManifest(string channelName, CancellationToken cancellationToken)
    {
        await EnsureSeedChannelsAsync(cancellationToken);
        var channel = (await ReadChannelsAsync(cancellationToken)).FirstOrDefault(item => item.Name.Equals(NormaliseChannelName(channelName), StringComparison.OrdinalIgnoreCase));
        if (channel is null || !channel.IsEnabled) return NotFound("Update channel is unavailable.");
        var packages = (await ReadPackagesAsync(channel.Name, cancellationToken))
            .Where(item => item.Status == "Published" && item.RevokedAtUtc is null)
            .OrderByDescending(item => item.PublishedAtUtc ?? item.CreatedAtUtc)
            .ToList();
        return Ok(new UpdateManifestDto(channel.Name, DateTime.UtcNow, packages));
    }

    [HttpPost("verify")]
    public ActionResult<VerifyUpdatePackageResponse> VerifyPackage(VerifyUpdatePackageRequest request)
    {
        return Ok(ValidatePackage(request.DownloadUrl, request.Sha256, request.SignatureAlgorithm, request.Signature, request.PublicKeyId));
    }

    private async Task EnsureSeedChannelsAsync(CancellationToken cancellationToken)
    {
        var count = Convert.ToInt32(await ScalarAsync("SELECT COUNT(*) FROM \"UpdateChannels\"", cancellationToken));
        if (count > 0) return;
        var now = DateTime.UtcNow;
        await ExecuteAsync("""
            INSERT INTO "UpdateChannels" ("Id", "Name", "Description", "IsEnabled", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@StableId, 'stable', 'Stable signed updates and rule bundles', true, @Now, @Now),
                   (@PreviewId, 'preview', 'Preview channel for controlled testing before stable release', true, @Now, @Now)
            """, cancellationToken,
            ("StableId", Guid.NewGuid()), ("PreviewId", Guid.NewGuid()), ("Now", now));
    }

    private static VerifyUpdatePackageResponse ValidatePackage(string downloadUrl, string sha256, string signatureAlgorithm, string signature, string publicKeyId)
    {
        var checks = new List<string>();
        var warnings = new List<string>();
        var isValid = true;

        if (Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            checks.Add("Download URL uses HTTPS.");
        }
        else
        {
            isValid = false;
            warnings.Add("Download URL must be absolute HTTPS.");
        }

        if (sha256.Length == 64 && sha256.All(Uri.IsHexDigit)) checks.Add("SHA256 hash format is valid.");
        else { isValid = false; warnings.Add("SHA256 must be a 64-character hexadecimal value."); }

        if (!string.IsNullOrWhiteSpace(signatureAlgorithm) && signatureAlgorithm.Contains("SHA", StringComparison.OrdinalIgnoreCase)) checks.Add("Signature algorithm is declared.");
        else { isValid = false; warnings.Add("Signature algorithm is missing or unsupported."); }

        if (!string.IsNullOrWhiteSpace(signature) && signature.Length >= 16) checks.Add("Signature value is present.");
        else { isValid = false; warnings.Add("Signature value is missing or too short."); }

        if (!string.IsNullOrWhiteSpace(publicKeyId)) checks.Add("Public key identifier is present.");
        else { isValid = false; warnings.Add("Public key identifier is required for trust decisions."); }

        return new VerifyUpdatePackageResponse(isValid, isValid ? "Package metadata is structurally valid for signed distribution." : "Package metadata failed signed distribution validation.", checks, warnings);
    }

    private async Task<List<UpdateChannelDto>> ReadChannelsAsync(CancellationToken cancellationToken)
    {
        var channels = new List<UpdateChannelDto>();
        await using var command = await CreateCommandAsync("SELECT * FROM \"UpdateChannels\" ORDER BY \"Name\"", cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            channels.Add(new UpdateChannelDto(GetGuid(reader, "Id"), GetString(reader, "Name"), GetString(reader, "Description"), GetBool(reader, "IsEnabled"), GetDate(reader, "CreatedAtUtc"), GetDate(reader, "UpdatedAtUtc")));
        }
        return channels;
    }

    private async Task<List<UpdatePackageDto>> ReadPackagesAsync(string? channelName, CancellationToken cancellationToken)
    {
        var sql = """
            SELECT p.*, c."Name" AS "ChannelName"
            FROM "UpdatePackages" p
            INNER JOIN "UpdateChannels" c ON c."Id" = p."ChannelId"
            """;
        var parameters = new List<(string Name, object? Value)>();
        if (!string.IsNullOrWhiteSpace(channelName))
        {
            sql += " WHERE c.\"Name\" = @ChannelName";
            parameters.Add(("ChannelName", NormaliseChannelName(channelName)));
        }
        sql += " ORDER BY p.\"CreatedAtUtc\" DESC LIMIT 250";

        var packages = new List<UpdatePackageDto>();
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters.ToArray());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            packages.Add(new UpdatePackageDto(
                GetGuid(reader, "Id"), GetString(reader, "ChannelName"), GetString(reader, "PackageType"), GetString(reader, "Name"), GetString(reader, "Version"),
                GetString(reader, "MinimumAppVersion"), GetString(reader, "TargetPlatform"), GetString(reader, "DownloadUrl"), GetString(reader, "Sha256"),
                GetLong(reader, "SizeBytes"), GetString(reader, "SignatureAlgorithm"), GetString(reader, "Signature"), GetString(reader, "PublicKeyId"), GetString(reader, "Status"),
                GetString(reader, "ReleaseNotes"), GetDate(reader, "CreatedAtUtc"), GetNullableDate(reader, "PublishedAtUtc"), GetNullableDate(reader, "RevokedAtUtc")));
        }
        return packages;
    }

    private static string NormaliseChannelName(string value) => string.IsNullOrWhiteSpace(value) ? "stable" : value.Trim().ToLowerInvariant();
    private static string NormaliseStatus(string value) => value.Equals("Published", StringComparison.OrdinalIgnoreCase) ? "Published" : value.Equals("Revoked", StringComparison.OrdinalIgnoreCase) ? "Revoked" : "Draft";

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
    private static long GetLong(IDataRecord record, string name) => record.GetInt64(record.GetOrdinal(name));
    private static DateTime GetDate(IDataRecord record, string name) => record.GetDateTime(record.GetOrdinal(name));
    private static DateTime? GetNullableDate(IDataRecord record, string name) => record.IsDBNull(record.GetOrdinal(name)) ? null : record.GetDateTime(record.GetOrdinal(name));
}
