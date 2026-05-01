using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

public partial class AddNormalisedSiemEventFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Action",
            table: "SiemEvents",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CloudResourceId",
            table: "SiemEvents",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CloudTenantId",
            table: "SiemEvents",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CommandLine",
            table: "SiemEvents",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "DestinationIp",
            table: "SiemEvents",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DestinationPort",
            table: "SiemEvents",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Domain",
            table: "SiemEvents",
            type: "character varying(253)",
            maxLength: 253,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EventCategory",
            table: "SiemEvents",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FileHashSha256",
            table: "SiemEvents",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FileName",
            table: "SiemEvents",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FilePath",
            table: "SiemEvents",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Mailbox",
            table: "SiemEvents",
            type: "character varying(320)",
            maxLength: 320,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Outcome",
            table: "SiemEvents",
            type: "character varying(80)",
            maxLength: 80,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ParentProcessName",
            table: "SiemEvents",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProcessName",
            table: "SiemEvents",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Product",
            table: "SiemEvents",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "SourceId",
            table: "SiemEvents",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceIp",
            table: "SiemEvents",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SourceName",
            table: "SiemEvents",
            type: "character varying(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "SourcePort",
            table: "SiemEvents",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Url",
            table: "SiemEvents",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "User",
            table: "SiemEvents",
            type: "character varying(320)",
            maxLength: 320,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Vendor",
            table: "SiemEvents",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "Source",
            table: "SiemEvents",
            type: "character varying(160)",
            maxLength: 160,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "Host",
            table: "SiemEvents",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "EventType",
            table: "SiemEvents",
            type: "character varying(160)",
            maxLength: 160,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.CreateIndex(name: "IX_SiemEvents_Action", table: "SiemEvents", column: "Action");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_DestinationIp", table: "SiemEvents", column: "DestinationIp");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_Domain", table: "SiemEvents", column: "Domain");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_EventCategory", table: "SiemEvents", column: "EventCategory");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_FileHashSha256", table: "SiemEvents", column: "FileHashSha256");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_SourceId", table: "SiemEvents", column: "SourceId");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_SourceIp", table: "SiemEvents", column: "SourceIp");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_SourceName", table: "SiemEvents", column: "SourceName");
        migrationBuilder.CreateIndex(name: "IX_SiemEvents_User", table: "SiemEvents", column: "User");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_SiemEvents_Action", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_DestinationIp", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_Domain", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_EventCategory", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_FileHashSha256", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_SourceId", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_SourceIp", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_SourceName", table: "SiemEvents");
        migrationBuilder.DropIndex(name: "IX_SiemEvents_User", table: "SiemEvents");

        migrationBuilder.DropColumn(name: "Action", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "CloudResourceId", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "CloudTenantId", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "CommandLine", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "DestinationIp", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "DestinationPort", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "Domain", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "EventCategory", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "FileHashSha256", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "FileName", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "FilePath", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "Mailbox", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "Outcome", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "ParentProcessName", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "ProcessName", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "Product", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "SourceId", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "SourceIp", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "SourceName", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "SourcePort", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "Url", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "User", table: "SiemEvents");
        migrationBuilder.DropColumn(name: "Vendor", table: "SiemEvents");

        migrationBuilder.AlterColumn<string>(
            name: "Source",
            table: "SiemEvents",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(160)",
            oldMaxLength: 160);

        migrationBuilder.AlterColumn<string>(
            name: "Host",
            table: "SiemEvents",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);

        migrationBuilder.AlterColumn<string>(
            name: "EventType",
            table: "SiemEvents",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(160)",
            oldMaxLength: 160);
    }
}
