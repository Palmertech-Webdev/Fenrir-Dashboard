using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260502000000_AddSignedUpdateDistribution")]
public partial class AddSignedUpdateDistribution : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "UpdateChannels",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_UpdateChannels", x => x.Id));

        migrationBuilder.CreateTable(
            name: "UpdatePackages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                PackageType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Name = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                MinimumAppVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                TargetPlatform = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                DownloadUrl = table.Column<string>(type: "text", nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                SignatureAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Signature = table.Column<string>(type: "text", nullable: false),
                PublicKeyId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                ReleaseNotes = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_UpdatePackages", x => x.Id));

        migrationBuilder.CreateIndex("IX_UpdateChannels_Name", "UpdateChannels", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_UpdateChannels_IsEnabled", "UpdateChannels", "IsEnabled");
        migrationBuilder.CreateIndex("IX_UpdatePackages_ChannelId", "UpdatePackages", "ChannelId");
        migrationBuilder.CreateIndex("IX_UpdatePackages_Status", "UpdatePackages", "Status");
        migrationBuilder.CreateIndex("IX_UpdatePackages_PackageType", "UpdatePackages", "PackageType");
        migrationBuilder.CreateIndex("IX_UpdatePackages_Version", "UpdatePackages", "Version");
        migrationBuilder.CreateIndex("IX_UpdatePackages_PublicKeyId", "UpdatePackages", "PublicKeyId");

        migrationBuilder.Sql(@"INSERT INTO ""UpdateChannels"" (""Id"", ""Name"", ""Description"", ""IsEnabled"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
            VALUES
            ('018f8df0-27ab-7b8d-b585-3fd0f7c2b101', 'stable', 'Stable signed updates and rule bundles', true, '2026-05-02T00:00:00Z', '2026-05-02T00:00:00Z'),
            ('018f8df0-27ab-7b8d-b585-3fd0f7c2b102', 'preview', 'Preview channel for controlled testing before stable release', true, '2026-05-02T00:00:00Z', '2026-05-02T00:00:00Z');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("UpdatePackages");
        migrationBuilder.DropTable("UpdateChannels");
    }
}
