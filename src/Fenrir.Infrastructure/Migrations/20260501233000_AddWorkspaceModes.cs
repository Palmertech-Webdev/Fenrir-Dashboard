using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501233000_AddWorkspaceModes")]
public partial class AddWorkspaceModes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WorkspaceModes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Mode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "text", nullable: false),
                ShowAdvancedFeatures = table.Column<bool>(type: "boolean", nullable: false),
                AllowResponseActions = table.Column<bool>(type: "boolean", nullable: false),
                AllowEvidenceExports = table.Column<bool>(type: "boolean", nullable: false),
                AllowSourceConfiguration = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_WorkspaceModes", x => x.Id));

        migrationBuilder.CreateIndex("IX_WorkspaceModes_UserKey", "WorkspaceModes", "UserKey", unique: true);
        migrationBuilder.CreateIndex("IX_WorkspaceModes_Mode", "WorkspaceModes", "Mode");

        migrationBuilder.Sql(@"INSERT INTO ""WorkspaceModes"" (""Id"", ""UserKey"", ""Mode"", ""Role"", ""DisplayName"", ""Description"", ""ShowAdvancedFeatures"", ""AllowResponseActions"", ""AllowEvidenceExports"", ""AllowSourceConfiguration"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
            VALUES ('018f8df0-27ab-7b8d-b585-3fd0f7c2b014', 'local', 'Analyst', 'Analyst', 'Analyst Mode', 'Full SOC investigation workspace with advanced telemetry, response workflows and configuration controls.', true, true, true, true, '2026-05-01T00:00:00Z', '2026-05-01T00:00:00Z');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("WorkspaceModes");
    }
}
