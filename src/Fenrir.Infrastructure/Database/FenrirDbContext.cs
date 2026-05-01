using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Infrastructure.Database;

public sealed class FenrirDbContext(DbContextOptions<FenrirDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AgentEnrolmentToken> AgentEnrolmentTokens => Set<AgentEnrolmentToken>();
    public DbSet<AgentEndpoint> AgentEndpoints => Set<AgentEndpoint>();
    public DbSet<Indicator> Indicators => Set<Indicator>();
    public DbSet<EmailCheck> EmailChecks => Set<EmailCheck>();
    public DbSet<EmailHeaderCheck> EmailHeaderChecks => Set<EmailHeaderCheck>();
    public DbSet<DnsCheck> DnsChecks => Set<DnsCheck>();
    public DbSet<DnsMonitoredDomain> DnsMonitoredDomains => Set<DnsMonitoredDomain>();
    public DbSet<DnsObservationEvent> DnsObservationEvents => Set<DnsObservationEvent>();
    public DbSet<DarkWebCheck> DarkWebChecks => Set<DarkWebCheck>();
    public DbSet<NetworkScan> NetworkScans => Set<NetworkScan>();
    public DbSet<NetworkScanResult> NetworkScanResults => Set<NetworkScanResult>();
    public DbSet<SecurityEvent> SiemEvents => Set<SecurityEvent>();
    public DbSet<SiemLogSource> SiemLogSources => Set<SiemLogSource>();
    public DbSet<SiemSourceConfig> SiemSourceConfigs => Set<SiemSourceConfig>();
    public DbSet<SiemSourceSecretRef> SiemSourceSecretRefs => Set<SiemSourceSecretRef>();
    public DbSet<SiemSourceState> SiemSourceStates => Set<SiemSourceState>();
    public DbSet<SiemSourceHealthSnapshot> SiemSourceHealthSnapshots => Set<SiemSourceHealthSnapshot>();
    public DbSet<SiemIngestionJob> SiemIngestionJobs => Set<SiemIngestionJob>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<JobRecord> Jobs => Set<JobRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.Email).HasMaxLength(320);
            entity.Property(user => user.Role).HasMaxLength(64);
        });

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("ApiKeys");
            entity.HasIndex(apiKey => apiKey.KeyHash).IsUnique();
            entity.Property(apiKey => apiKey.Name).HasMaxLength(128);
            entity.Property(apiKey => apiKey.Role).HasMaxLength(64);
        });

        modelBuilder.Entity<Asset>(entity =>
        {
            entity.ToTable("Assets");
            entity.HasIndex(asset => asset.Name);
            entity.HasIndex(asset => asset.IpAddress);
            entity.Property(asset => asset.AssetType).HasMaxLength(64);
        });

        modelBuilder.Entity<AgentEnrolmentToken>(entity =>
        {
            entity.ToTable("AgentEnrolmentTokens");
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.HasIndex(token => token.ExpiresAtUtc);
            entity.HasIndex(token => token.RevokedAtUtc);
            entity.Property(token => token.TokenHash).HasMaxLength(128);
            entity.Property(token => token.Name).HasMaxLength(160);
            entity.Property(token => token.AllowedHostPattern).HasMaxLength(255);
        });

        modelBuilder.Entity<AgentEndpoint>(entity =>
        {
            entity.ToTable("AgentEndpoints");
            entity.HasIndex(agent => agent.AgentId).IsUnique();
            entity.HasIndex(agent => agent.MachineGuid).IsUnique();
            entity.HasIndex(agent => agent.Hostname);
            entity.HasIndex(agent => agent.Status);
            entity.HasIndex(agent => agent.SourceId);
            entity.HasIndex(agent => agent.LastHeartbeatAtUtc);
            entity.Property(agent => agent.AgentId).HasMaxLength(120);
            entity.Property(agent => agent.Hostname).HasMaxLength(255);
            entity.Property(agent => agent.MachineGuid).HasMaxLength(160);
            entity.Property(agent => agent.OperatingSystem).HasMaxLength(160);
            entity.Property(agent => agent.AgentVersion).HasMaxLength(80);
            entity.Property(agent => agent.Status).HasMaxLength(80);
            entity.Property(agent => agent.IpAddress).HasMaxLength(64);
        });

        modelBuilder.Entity<Indicator>(entity =>
        {
            entity.ToTable("Indicators");
            entity.HasIndex(indicator => indicator.NormalizedValue).IsUnique();
            entity.HasIndex(indicator => indicator.Type);
            entity.HasIndex(indicator => indicator.Verdict);
            entity.Property(indicator => indicator.Type).HasMaxLength(64);
            entity.Property(indicator => indicator.Verdict).HasMaxLength(64);
            entity.Property(indicator => indicator.Severity).HasMaxLength(64);
        });

        modelBuilder.Entity<Finding>(entity =>
        {
            entity.ToTable("Findings");
            entity.HasIndex(finding => finding.Module);
            entity.HasIndex(finding => finding.Severity);
            entity.HasIndex(finding => finding.Status);
            entity.Property(finding => finding.Module).HasMaxLength(128);
            entity.Property(finding => finding.Type).HasMaxLength(128);
            entity.Property(finding => finding.Severity).HasMaxLength(64);
            entity.Property(finding => finding.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<EmailCheck>(entity =>
        {
            entity.ToTable("EmailChecks");
            entity.HasIndex(check => check.Domain);
            entity.Property(check => check.Email).HasMaxLength(320);
            entity.Property(check => check.Risk).HasMaxLength(64);
        });

        modelBuilder.Entity<EmailHeaderCheck>(entity =>
        {
            entity.ToTable("EmailHeaderChecks");
            entity.HasIndex(check => check.Risk);
            entity.Property(check => check.Risk).HasMaxLength(64);
        });

        modelBuilder.Entity<DnsCheck>(entity =>
        {
            entity.ToTable("DnsChecks");
            entity.HasIndex(check => check.Domain);
            entity.Property(check => check.Domain).HasMaxLength(253);
            entity.Property(check => check.Risk).HasMaxLength(64);
        });

        modelBuilder.Entity<DnsMonitoredDomain>(entity =>
        {
            entity.ToTable("DnsMonitoredDomains");
            entity.HasIndex(domain => domain.Domain).IsUnique();
            entity.Property(domain => domain.Domain).HasMaxLength(253);
        });

        modelBuilder.Entity<DnsObservationEvent>(entity =>
        {
            entity.ToTable("DnsObservationEvents");
            entity.HasIndex(observation => observation.QueriedDomain);
            entity.HasIndex(observation => observation.TimestampUtc);
            entity.Property(observation => observation.Verdict).HasMaxLength(64);
        });

        modelBuilder.Entity<DarkWebCheck>(entity =>
        {
            entity.ToTable("DarkWebChecks");
            entity.HasIndex(check => check.Query);
            entity.Property(check => check.QueryType).HasMaxLength(64);
        });

        modelBuilder.Entity<NetworkScan>(entity =>
        {
            entity.ToTable("NetworkScans");
            entity.HasIndex(scan => scan.Target);
            entity.HasIndex(scan => scan.Status);
            entity.Property(scan => scan.ScanType).HasMaxLength(64);
            entity.Property(scan => scan.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<NetworkScanResult>(entity =>
        {
            entity.ToTable("NetworkScanResults");
            entity.HasIndex(result => new { result.NetworkScanId, result.Asset, result.Port });
            entity.Property(result => result.Severity).HasMaxLength(64);
        });

        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.ToTable("SiemEvents");
            entity.HasIndex(securityEvent => securityEvent.TimestampUtc);
            entity.HasIndex(securityEvent => securityEvent.SourceId);
            entity.HasIndex(securityEvent => securityEvent.Source);
            entity.HasIndex(securityEvent => securityEvent.SourceName);
            entity.HasIndex(securityEvent => securityEvent.Host);
            entity.HasIndex(securityEvent => securityEvent.EventType);
            entity.HasIndex(securityEvent => securityEvent.EventCategory);
            entity.HasIndex(securityEvent => securityEvent.Severity);
            entity.HasIndex(securityEvent => securityEvent.User);
            entity.HasIndex(securityEvent => securityEvent.SourceIp);
            entity.HasIndex(securityEvent => securityEvent.DestinationIp);
            entity.HasIndex(securityEvent => securityEvent.Domain);
            entity.HasIndex(securityEvent => securityEvent.FileHashSha256);
            entity.HasIndex(securityEvent => securityEvent.Action);
            entity.Property(securityEvent => securityEvent.Source).HasMaxLength(160);
            entity.Property(securityEvent => securityEvent.SourceName).HasMaxLength(160);
            entity.Property(securityEvent => securityEvent.Vendor).HasMaxLength(100);
            entity.Property(securityEvent => securityEvent.Product).HasMaxLength(120);
            entity.Property(securityEvent => securityEvent.Host).HasMaxLength(255);
            entity.Property(securityEvent => securityEvent.EventType).HasMaxLength(160);
            entity.Property(securityEvent => securityEvent.EventCategory).HasMaxLength(120);
            entity.Property(securityEvent => securityEvent.Severity).HasMaxLength(64);
            entity.Property(securityEvent => securityEvent.User).HasMaxLength(320);
            entity.Property(securityEvent => securityEvent.SourceIp).HasMaxLength(64);
            entity.Property(securityEvent => securityEvent.DestinationIp).HasMaxLength(64);
            entity.Property(securityEvent => securityEvent.Domain).HasMaxLength(253);
            entity.Property(securityEvent => securityEvent.Url).HasMaxLength(2048);
            entity.Property(securityEvent => securityEvent.FileHashSha256).HasMaxLength(64);
            entity.Property(securityEvent => securityEvent.Mailbox).HasMaxLength(320);
            entity.Property(securityEvent => securityEvent.CloudTenantId).HasMaxLength(160);
            entity.Property(securityEvent => securityEvent.Action).HasMaxLength(160);
            entity.Property(securityEvent => securityEvent.Outcome).HasMaxLength(80);
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                entity.Property(securityEvent => securityEvent.RawJson).HasColumnType("jsonb");
            }
        });

        modelBuilder.Entity<SiemLogSource>(entity =>
        {
            entity.ToTable("SiemLogSources");
            entity.HasIndex(source => source.Name).IsUnique();
            entity.HasIndex(source => source.SourceType);
            entity.HasIndex(source => source.Status);
            entity.Property(source => source.Name).HasMaxLength(160);
            entity.Property(source => source.SourceType).HasMaxLength(80);
            entity.Property(source => source.Vendor).HasMaxLength(100);
            entity.Property(source => source.Product).HasMaxLength(120);
            entity.Property(source => source.ConnectionType).HasMaxLength(80);
            entity.Property(source => source.Parser).HasMaxLength(120);
            entity.Property(source => source.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<SiemSourceConfig>(entity =>
        {
            entity.ToTable("SiemSourceConfigs");
            entity.HasIndex(config => config.SourceId).IsUnique();
            entity.Property(config => config.EndpointUrl).HasMaxLength(2048);
            entity.Property(config => config.TenantId).HasMaxLength(160);
            entity.Property(config => config.Region).HasMaxLength(80);
            entity.Property(config => config.BucketName).HasMaxLength(255);
            entity.Property(config => config.StreamName).HasMaxLength(255);
            if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
            {
                entity.Property(config => config.ConfigJson).HasColumnType("jsonb");
            }

            entity.HasOne(config => config.Source)
                .WithOne(source => source.Config)
                .HasForeignKey<SiemSourceConfig>(config => config.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiemSourceSecretRef>(entity =>
        {
            entity.ToTable("SiemSourceSecretRefs");
            entity.HasIndex(secret => new { secret.SourceId, secret.SecretPurpose }).IsUnique();
            entity.Property(secret => secret.SecretPurpose).HasMaxLength(120);
            entity.Property(secret => secret.SecretProvider).HasMaxLength(120);
            entity.Property(secret => secret.SecretKey).HasMaxLength(512);
            entity.HasOne(secret => secret.Source)
                .WithMany(source => source.SecretRefs)
                .HasForeignKey(secret => secret.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiemSourceState>(entity =>
        {
            entity.ToTable("SiemSourceStates");
            entity.HasIndex(state => state.SourceId).IsUnique();
            entity.HasIndex(state => state.ConnectorState);
            entity.Property(state => state.ConnectorState).HasMaxLength(80);
            entity.HasOne(state => state.Source)
                .WithOne(source => source.State)
                .HasForeignKey<SiemSourceState>(state => state.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiemSourceHealthSnapshot>(entity =>
        {
            entity.ToTable("SiemSourceHealthSnapshots");
            entity.HasIndex(snapshot => snapshot.SourceId);
            entity.HasIndex(snapshot => snapshot.CapturedAtUtc);
            entity.HasIndex(snapshot => snapshot.Status);
            entity.Property(snapshot => snapshot.Status).HasMaxLength(80);
            entity.HasOne(snapshot => snapshot.Source)
                .WithMany(source => source.HealthSnapshots)
                .HasForeignKey(snapshot => snapshot.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SiemIngestionJob>(entity =>
        {
            entity.ToTable("SiemIngestionJobs");
            entity.HasIndex(job => job.Status);
            entity.HasIndex(job => job.SourceId);
            entity.HasIndex(job => job.StartedAtUtc);
            entity.Property(job => job.SourceName).HasMaxLength(160);
            entity.Property(job => job.InputType).HasMaxLength(64);
            entity.Property(job => job.Parser).HasMaxLength(120);
            entity.Property(job => job.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<JobRecord>(entity =>
        {
            entity.ToTable("Jobs");
            entity.HasIndex(job => job.Status);
            entity.HasIndex(job => job.JobType);
            entity.Property(job => job.JobType).HasMaxLength(128);
            entity.Property(job => job.Status).HasMaxLength(64);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(auditLog => auditLog.CreatedAtUtc);
        });

        SeedDefaults(modelBuilder);
    }

    private static void SeedDefaults(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = Guid.Parse("018f8df0-27ab-7b8d-b585-3fd0f7c2a001"),
            Email = "admin@fenrir.local",
            DisplayName = "Fenrir Admin",
            Role = "Admin",
            IsActive = true,
            CreatedAtUtc = new DateTime(2026, 05, 01, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
