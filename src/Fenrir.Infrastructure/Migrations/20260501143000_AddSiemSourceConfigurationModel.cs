using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501143000_AddSiemSourceConfigurationModel")]
public partial class AddSiemSourceConfigurationModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SiemSourceConfigs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                PollingIntervalSeconds = table.Column<int>(type: "integer", nullable: false),
                EndpointUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                TenantId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                Region = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                BucketName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                StreamName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                QueryFilter = table.Column<string>(type: "text", nullable: true),
                MaxBatchSize = table.Column<int>(type: "integer", nullable: false),
                EnabledFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemSourceConfigs", x => x.Id);
                table.ForeignKey(
                    name: "FK_SiemSourceConfigs_SiemLogSources_SourceId",
                    column: x => x.SourceId,
                    principalTable: "SiemLogSources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SiemSourceHealthSnapshots",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                EventsReceivedLastInterval = table.Column<int>(type: "integer", nullable: false),
                EventsParsedLastInterval = table.Column<int>(type: "integer", nullable: false),
                EventsFailedLastInterval = table.Column<int>(type: "integer", nullable: false),
                ParseFailureRate = table.Column<double>(type: "double precision", nullable: false),
                LagSeconds = table.Column<int>(type: "integer", nullable: false),
                Message = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemSourceHealthSnapshots", x => x.Id);
                table.ForeignKey(
                    name: "FK_SiemSourceHealthSnapshots_SiemLogSources_SourceId",
                    column: x => x.SourceId,
                    principalTable: "SiemLogSources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SiemSourceSecretRefs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                SecretPurpose = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                SecretProvider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                SecretKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemSourceSecretRefs", x => x.Id);
                table.ForeignKey(
                    name: "FK_SiemSourceSecretRefs_SiemLogSources_SourceId",
                    column: x => x.SourceId,
                    principalTable: "SiemLogSources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SiemSourceStates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                ConnectorState = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CursorValue = table.Column<string>(type: "text", nullable: true),
                LastPollStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastPollCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastEventTimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NextPollAfterUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ConsecutiveFailureCount = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemSourceStates", x => x.Id);
                table.ForeignKey(
                    name: "FK_SiemSourceStates_SiemLogSources_SourceId",
                    column: x => x.SourceId,
                    principalTable: "SiemLogSources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceConfigs_SourceId",
            table: "SiemSourceConfigs",
            column: "SourceId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceHealthSnapshots_CapturedAtUtc",
            table: "SiemSourceHealthSnapshots",
            column: "CapturedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceHealthSnapshots_SourceId",
            table: "SiemSourceHealthSnapshots",
            column: "SourceId");

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceHealthSnapshots_Status",
            table: "SiemSourceHealthSnapshots",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceSecretRefs_SourceId_SecretPurpose",
            table: "SiemSourceSecretRefs",
            columns: new[] { "SourceId", "SecretPurpose" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceStates_ConnectorState",
            table: "SiemSourceStates",
            column: "ConnectorState");

        migrationBuilder.CreateIndex(
            name: "IX_SiemSourceStates_SourceId",
            table: "SiemSourceStates",
            column: "SourceId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiemSourceConfigs");
        migrationBuilder.DropTable(name: "SiemSourceHealthSnapshots");
        migrationBuilder.DropTable(name: "SiemSourceSecretRefs");
        migrationBuilder.DropTable(name: "SiemSourceStates");
    }
}
