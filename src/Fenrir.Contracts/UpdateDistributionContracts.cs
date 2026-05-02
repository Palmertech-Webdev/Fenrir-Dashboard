namespace Fenrir.Contracts;

public sealed record UpdateChannelDto(
    Guid Id,
    string Name,
    string Description,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record UpdatePackageDto(
    Guid Id,
    string ChannelName,
    string PackageType,
    string Name,
    string Version,
    string MinimumAppVersion,
    string TargetPlatform,
    string DownloadUrl,
    string Sha256,
    long SizeBytes,
    string SignatureAlgorithm,
    string Signature,
    string PublicKeyId,
    string Status,
    string ReleaseNotes,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? RevokedAtUtc);

public sealed record CreateUpdateChannelRequest(
    string Name,
    string Description = "",
    bool IsEnabled = true);

public sealed record CreateUpdatePackageRequest(
    string ChannelName,
    string PackageType,
    string Name,
    string Version,
    string MinimumAppVersion,
    string TargetPlatform,
    string DownloadUrl,
    string Sha256,
    long SizeBytes,
    string SignatureAlgorithm,
    string Signature,
    string PublicKeyId,
    string ReleaseNotes = "",
    string Status = "Draft");

public sealed record UpdateManifestDto(
    string ChannelName,
    DateTime GeneratedAtUtc,
    IReadOnlyList<UpdatePackageDto> Packages);

public sealed record VerifyUpdatePackageRequest(
    string DownloadUrl,
    string Sha256,
    string SignatureAlgorithm,
    string Signature,
    string PublicKeyId);

public sealed record VerifyUpdatePackageResponse(
    bool IsValid,
    string Verdict,
    IReadOnlyList<string> Checks,
    IReadOnlyList<string> Warnings);
