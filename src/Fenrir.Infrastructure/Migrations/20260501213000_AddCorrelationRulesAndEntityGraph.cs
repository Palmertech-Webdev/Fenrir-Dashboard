using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501213000_AddCorrelationRulesAndEntityGraph")]
public partial class AddCorrelationRulesAndEntityGraph : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CorrelationRules",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Enabled = table.Column<bool>(type: "boolean", nullable: false),
                RuleType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                QueryDefinition = table.Column<string>(type: "text", nullable: false),
                TimeWindowMinutes = table.Column<int>(type: "integer", nullable: false),
                GroupByFields = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Threshold = table.Column<int>(type: "integer", nullable: false),
                MitreTactic = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                MitreTechnique = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CorrelationRules", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CorrelationAlerts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RuleId = table.Column<Guid>(type: "uuid", nullable: true),
                RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                FirstSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EventIdsJson = table.Column<string>(type: "jsonb", nullable: false),
                EntitySummaryJson = table.Column<string>(type: "jsonb", nullable: false),
                MitreTactic = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                MitreTechnique = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CorrelationAlerts", x => x.Id);
            });

        migrationBuilder.CreateIndex("IX_CorrelationRules_Enabled", "CorrelationRules", "Enabled");
        migrationBuilder.CreateIndex("IX_CorrelationRules_RuleType", "CorrelationRules", "RuleType");
        migrationBuilder.CreateIndex("IX_CorrelationRules_Severity", "CorrelationRules", "Severity");
        migrationBuilder.CreateIndex("IX_CorrelationAlerts_RuleId", "CorrelationAlerts", "RuleId");
        migrationBuilder.CreateIndex("IX_CorrelationAlerts_Severity", "CorrelationAlerts", "Severity");
        migrationBuilder.CreateIndex("IX_CorrelationAlerts_Status", "CorrelationAlerts", "Status");
        migrationBuilder.CreateIndex("IX_CorrelationAlerts_CreatedAtUtc", "CorrelationAlerts", "CreatedAtUtc");
        migrationBuilder.CreateIndex("IX_CorrelationAlerts_LastSeenUtc", "CorrelationAlerts", "LastSeenUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CorrelationAlerts");
        migrationBuilder.DropTable("CorrelationRules");
    }
}
