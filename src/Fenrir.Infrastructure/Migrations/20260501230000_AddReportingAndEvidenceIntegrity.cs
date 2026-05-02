using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501230000_AddReportingAndEvidenceIntegrity")]
public partial class AddReportingAndEvidenceIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InvestigationReports",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                ReportType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Scope = table.Column<string>(type: "text", nullable: true),
                RequestedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SignatureAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_InvestigationReports", x => x.Id));

        migrationBuilder.CreateTable(
            name: "EvidenceIntegrityRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                EntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                EntityId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SignatureAlgorithm = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                SealedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                SealedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EvidenceIntegrityRecords", x => x.Id));

        migrationBuilder.CreateIndex("IX_InvestigationReports_CaseId", "InvestigationReports", "CaseId");
        migrationBuilder.CreateIndex("IX_InvestigationReports_Status", "InvestigationReports", "Status");
        migrationBuilder.CreateIndex("IX_InvestigationReports_CreatedAtUtc", "InvestigationReports", "CreatedAtUtc");
        migrationBuilder.CreateIndex("IX_EvidenceIntegrityRecords_Entity", "EvidenceIntegrityRecords", new[] { "EntityType", "EntityId" });
        migrationBuilder.CreateIndex("IX_EvidenceIntegrityRecords_SealedAtUtc", "EvidenceIntegrityRecords", "SealedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("EvidenceIntegrityRecords");
        migrationBuilder.DropTable("InvestigationReports");
    }
}
