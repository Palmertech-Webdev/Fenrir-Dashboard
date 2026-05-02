using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501220000_AddResponsePlaybooks")]
public partial class AddResponsePlaybooks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ResponsePlaybooks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TriggerType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                MitreTactic = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                MitreTechnique = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ResponsePlaybooks", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ResponsePlaybookSteps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PlaybookId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                ActionType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                TargetType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                CommandPreview = table.Column<string>(type: "text", nullable: true),
                IntegrationKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ResponsePlaybookSteps", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ResponsePlaybookRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PlaybookId = table.Column<Guid>(type: "uuid", nullable: false),
                PlaybookName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                AlertId = table.Column<Guid>(type: "uuid", nullable: true),
                EventId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                StartedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_ResponsePlaybookRuns", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ResponsePlaybookRunSteps",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RunId = table.Column<Guid>(type: "uuid", nullable: false),
                PlaybookStepId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Result = table.Column<string>(type: "text", nullable: true),
                ExecutedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                ExecutedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RequiresApproval = table.Column<bool>(type: "boolean", nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ResponsePlaybookRunSteps", x => x.Id));

        migrationBuilder.CreateIndex("IX_ResponsePlaybooks_Category", "ResponsePlaybooks", "Category");
        migrationBuilder.CreateIndex("IX_ResponsePlaybooks_Severity", "ResponsePlaybooks", "Severity");
        migrationBuilder.CreateIndex("IX_ResponsePlaybooks_IsEnabled", "ResponsePlaybooks", "IsEnabled");
        migrationBuilder.CreateIndex("IX_ResponsePlaybookSteps_PlaybookId", "ResponsePlaybookSteps", "PlaybookId");
        migrationBuilder.CreateIndex("IX_ResponsePlaybookRuns_PlaybookId", "ResponsePlaybookRuns", "PlaybookId");
        migrationBuilder.CreateIndex("IX_ResponsePlaybookRuns_CaseId", "ResponsePlaybookRuns", "CaseId");
        migrationBuilder.CreateIndex("IX_ResponsePlaybookRuns_AlertId", "ResponsePlaybookRuns", "AlertId");
        migrationBuilder.CreateIndex("IX_ResponsePlaybookRuns_Status", "ResponsePlaybookRuns", "Status");
        migrationBuilder.CreateIndex("IX_ResponsePlaybookRunSteps_RunId", "ResponsePlaybookRunSteps", "RunId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ResponsePlaybookRunSteps");
        migrationBuilder.DropTable("ResponsePlaybookRuns");
        migrationBuilder.DropTable("ResponsePlaybookSteps");
        migrationBuilder.DropTable("ResponsePlaybooks");
    }
}
