using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501172000_AddThreatIntelSourceAndIndicatorEnrichment")]
public partial class AddThreatIntelSourceAndIndicatorEnrichment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Indicators",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiresAtUtc",
            table: "Indicators",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExternalReference",
            table: "Indicators",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Tlp",
            table: "Indicators",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ThreatIntelSources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EndpointUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                SecretRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                LastSyncStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastSyncCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastSyncStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                LastError = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ThreatIntelSources", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Indicators_ExpiresAtUtc",
            table: "Indicators",
            column: "ExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Indicators_Severity",
            table: "Indicators",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_Indicators_Source",
            table: "Indicators",
            column: "Source");

        migrationBuilder.CreateIndex(
            name: "IX_ThreatIntelSources_IsEnabled",
            table: "ThreatIntelSources",
            column: "IsEnabled");

        migrationBuilder.CreateIndex(
            name: "IX_ThreatIntelSources_Name",
            table: "ThreatIntelSources",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ThreatIntelSources_SourceType",
            table: "ThreatIntelSources",
            column: "SourceType");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ThreatIntelSources");

        migrationBuilder.DropIndex(name: "IX_Indicators_ExpiresAtUtc", table: "Indicators");
        migrationBuilder.DropIndex(name: "IX_Indicators_Severity", table: "Indicators");
        migrationBuilder.DropIndex(name: "IX_Indicators_Source", table: "Indicators");

        migrationBuilder.DropColumn(name: "Description", table: "Indicators");
        migrationBuilder.DropColumn(name: "ExpiresAtUtc", table: "Indicators");
        migrationBuilder.DropColumn(name: "ExternalReference", table: "Indicators");
        migrationBuilder.DropColumn(name: "Tlp", table: "Indicators");
    }
}
