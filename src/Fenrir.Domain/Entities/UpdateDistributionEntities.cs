namespace Fenrir.Domain.Entities;

public class UpdateChannel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "stable";
    public string Description { get; set; } = "Stable update channel";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class UpdatePackage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChannelId { get; set; }
    public string PackageType { get; set; } = "Rules";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "0.0.1";
    public string MinimumAppVersion { get; set; } = "0.0.1";
    public string TargetPlatform { get; set; } = "any";
    public string DownloadUrl { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public long SizeBytes { get; set; }
    public string SignatureAlgorithm { get; set; } = "SHA256";
    public string Signature { get; set; } = "";
    public string PublicKeyId { get; set; } = "local-dev-key";
    public string Status { get; set; } = "Draft";
    public string ReleaseNotes { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
}
