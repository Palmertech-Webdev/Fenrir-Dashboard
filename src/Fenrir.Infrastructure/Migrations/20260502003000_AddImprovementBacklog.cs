using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260502003000_AddImprovementBacklog")]
public partial class AddImprovementBacklog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ImprovementBacklogItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                Area = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                Status = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ImprovementBacklogItems", x => x.Id));

        migrationBuilder.CreateIndex("IX_ImprovementBacklogItems_Area", "ImprovementBacklogItems", "Area");
        migrationBuilder.CreateIndex("IX_ImprovementBacklogItems_Priority", "ImprovementBacklogItems", "Priority");
        migrationBuilder.CreateIndex("IX_ImprovementBacklogItems_Status", "ImprovementBacklogItems", "Status");
        migrationBuilder.CreateIndex("IX_ImprovementBacklogItems_CreatedAtUtc", "ImprovementBacklogItems", "CreatedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ImprovementBacklogItems");
    }
}
