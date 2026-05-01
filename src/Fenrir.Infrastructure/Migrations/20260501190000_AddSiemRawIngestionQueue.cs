using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501190000_AddSiemRawIngestionQueue")]
public partial class AddSiemRawIngestionQueue : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SiemRawIngestionBatches",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                JobId = table.Column<Guid>(type: "uuid", nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                SourceName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                InputType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Parser = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                EventsReceived = table.Column<int>(type: "integer", nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ProcessingStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AttemptCount = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SiemRawIngestionBatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_SiemRawIngestionBatches_SiemIngestionJobs_JobId",
                    column: x => x.JobId,
                    principalTable: "SiemIngestionJobs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SiemRawIngestionBatches_CreatedAtUtc",
            table: "SiemRawIngestionBatches",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_SiemRawIngestionBatches_JobId",
            table: "SiemRawIngestionBatches",
            column: "JobId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SiemRawIngestionBatches_SourceId",
            table: "SiemRawIngestionBatches",
            column: "SourceId");

        migrationBuilder.CreateIndex(
            name: "IX_SiemRawIngestionBatches_Status",
            table: "SiemRawIngestionBatches",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "SiemRawIngestionBatches");
    }
}
