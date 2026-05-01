using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSiemIngestionTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SiemEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Source = table.Column<string>(type: "text", nullable: false),
                Host = table.Column<string>(type: "text", nullable: false),
                EventType = table.Column<string>(type: "text", nullable: false),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Message = table.Column<string>(type: "text", nullable: false),
                RawJson = table.Column<string>(type: "jsonb", nullable: false),
                IngestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemEvents", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SiemIngestionJobs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                InputType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Parser = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                EventsReceived = table.Column<int>(type: "integer", nullable: false),
                EventsParsed = table.Column<int>(type: "integer", nullable: false),
                EventsFailed = table.Column<int>(type: "integer", nullable: false),
                ErrorSummary = table.Column<string>(type: "text", nullable: true),
                StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemIngestionJobs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SiemLogSources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Vendor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Product = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ConnectionType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Parser = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastSuccessfulIngestAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemLogSources", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SiemEvents_EventType",
            table: "SiemEvents",
            column: "EventType");

        migrationBuilder.CreateIndex(
            name: "IX_SiemEvents_Host",
            table: "SiemEvents",
            column: "Host");

        migrationBuilder.CreateIndex(
            name: "IX_SiemEvents_Severity",
            table: "SiemEvents",
            column: "Severity");

        migrationBuilder.CreateIndex(
            name: "IX_SiemEvents_Source",
            table: "SiemEvents",
            column: "Source");

        migrationBuilder.CreateIndex(
            name: "IX_SiemEvents_TimestampUtc",
            table: "SiemEvents",
            column: "TimestampUtc");

        migrationBuilder.CreateIndex(
            name: "IX_SiemIngestionJobs_SourceId",
            table: "SiemIngestionJobs",
            column: "SourceId");

        migrationBuilder.CreateIndex(
            name: "IX_SiemIngestionJobs_StartedAtUtc",
            table: "SiemIngestionJobs",
            column: "StartedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_SiemIngestionJobs_Status",
            table: "SiemIngestionJobs",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_SiemLogSources_Name",
            table: "SiemLogSources",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SiemLogSources_SourceType",
            table: "SiemLogSources",
            column: "SourceType");

        migrationBuilder.CreateIndex(
            name: "IX_SiemLogSources_Status",
            table: "SiemLogSources",
            column: "Status");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiemEvents");
        migrationBuilder.DropTable(name: "SiemIngestionJobs");
        migrationBuilder.DropTable(name: "SiemLogSources");
    }
}
