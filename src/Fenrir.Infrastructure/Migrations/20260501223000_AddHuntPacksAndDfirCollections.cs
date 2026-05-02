using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501223000_AddHuntPacksAndDfirCollections")]
public partial class AddHuntPacksAndDfirCollections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HuntPacks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                MitreTactic = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                MitreTechnique = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_HuntPacks", x => x.Id));

        migrationBuilder.CreateTable(
            name: "HuntQueries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HuntPackId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                QueryType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                QueryDefinition = table.Column<string>(type: "text", nullable: false),
                TargetField = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                ExpectedEvidence = table.Column<string>(type: "text", nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_HuntQueries", x => x.Id));

        migrationBuilder.CreateTable(
            name: "HuntRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HuntPackId = table.Column<Guid>(type: "uuid", nullable: false),
                HuntPackName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                LookbackHours = table.Column<int>(type: "integer", nullable: false),
                StartedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Scope = table.Column<string>(type: "text", nullable: true),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Matches = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_HuntRuns", x => x.Id));

        migrationBuilder.CreateTable(
            name: "HuntRunResults",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                HuntRunId = table.Column<Guid>(type: "uuid", nullable: false),
                HuntQueryId = table.Column<Guid>(type: "uuid", nullable: false),
                QueryName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: true),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Summary = table.Column<string>(type: "text", nullable: false),
                Evidence = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_HuntRunResults", x => x.Id));

        migrationBuilder.CreateTable(
            name: "DfirCollections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                CollectionType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                RequestedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                ArtefactsJson = table.Column<string>(type: "text", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EvidenceBundlePath = table.Column<string>(type: "text", nullable: true),
                ErrorSummary = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_DfirCollections", x => x.Id));

        migrationBuilder.CreateIndex("IX_HuntPacks_Category", "HuntPacks", "Category");
        migrationBuilder.CreateIndex("IX_HuntPacks_Severity", "HuntPacks", "Severity");
        migrationBuilder.CreateIndex("IX_HuntPacks_IsEnabled", "HuntPacks", "IsEnabled");
        migrationBuilder.CreateIndex("IX_HuntQueries_HuntPackId", "HuntQueries", "HuntPackId");
        migrationBuilder.CreateIndex("IX_HuntRuns_HuntPackId", "HuntRuns", "HuntPackId");
        migrationBuilder.CreateIndex("IX_HuntRuns_Status", "HuntRuns", "Status");
        migrationBuilder.CreateIndex("IX_HuntRuns_CaseId", "HuntRuns", "CaseId");
        migrationBuilder.CreateIndex("IX_HuntRunResults_HuntRunId", "HuntRunResults", "HuntRunId");
        migrationBuilder.CreateIndex("IX_DfirCollections_Hostname", "DfirCollections", "Hostname");
        migrationBuilder.CreateIndex("IX_DfirCollections_Status", "DfirCollections", "Status");
        migrationBuilder.CreateIndex("IX_DfirCollections_CaseId", "DfirCollections", "CaseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("DfirCollections");
        migrationBuilder.DropTable("HuntRunResults");
        migrationBuilder.DropTable("HuntRuns");
        migrationBuilder.DropTable("HuntQueries");
        migrationBuilder.DropTable("HuntPacks");
    }
}
