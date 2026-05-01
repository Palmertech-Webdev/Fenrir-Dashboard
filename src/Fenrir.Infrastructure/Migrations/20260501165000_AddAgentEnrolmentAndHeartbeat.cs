using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501165000_AddAgentEnrolmentAndHeartbeat")]
public partial class AddAgentEnrolmentAndHeartbeat : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AgentEnrolmentTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                AllowedHostPattern = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                MaxUses = table.Column<int>(type: "integer", nullable: true),
                UseCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentEnrolmentTokens", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AgentEndpoints",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AgentId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Hostname = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                MachineGuid = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                OperatingSystem = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                AgentVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                FirstSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastHeartbeatAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastTelemetryAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                QueuedEventsCount = table.Column<int>(type: "integer", nullable: true),
                IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentEndpoints", x => x.Id);
            });

        migrationBuilder.CreateIndex(name: "IX_AgentEnrolmentTokens_TokenHash", table: "AgentEnrolmentTokens", column: "TokenHash", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AgentEnrolmentTokens_ExpiresAtUtc", table: "AgentEnrolmentTokens", column: "ExpiresAtUtc");
        migrationBuilder.CreateIndex(name: "IX_AgentEnrolmentTokens_RevokedAtUtc", table: "AgentEnrolmentTokens", column: "RevokedAtUtc");

        migrationBuilder.CreateIndex(name: "IX_AgentEndpoints_AgentId", table: "AgentEndpoints", column: "AgentId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AgentEndpoints_MachineGuid", table: "AgentEndpoints", column: "MachineGuid", unique: true);
        migrationBuilder.CreateIndex(name: "IX_AgentEndpoints_Hostname", table: "AgentEndpoints", column: "Hostname");
        migrationBuilder.CreateIndex(name: "IX_AgentEndpoints_Status", table: "AgentEndpoints", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_AgentEndpoints_SourceId", table: "AgentEndpoints", column: "SourceId");
        migrationBuilder.CreateIndex(name: "IX_AgentEndpoints_LastHeartbeatAtUtc", table: "AgentEndpoints", column: "LastHeartbeatAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AgentEndpoints");
        migrationBuilder.DropTable(name: "AgentEnrolmentTokens");
    }
}
