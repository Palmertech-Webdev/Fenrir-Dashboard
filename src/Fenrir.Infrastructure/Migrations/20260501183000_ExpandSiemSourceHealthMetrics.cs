using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501183000_ExpandSiemSourceHealthMetrics")]
public partial class ExpandSiemSourceHealthMetrics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "AverageIngestLatencyMs",
            table: "SiemSourceHealthSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "EventsFailedLast15Minutes",
            table: "SiemSourceHealthSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "EventsParsedLast15Minutes",
            table: "SiemSourceHealthSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "EventsReceivedLast15Minutes",
            table: "SiemSourceHealthSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<string>(
            name: "LastError",
            table: "SiemSourceHealthSnapshots",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastPollAtUtc",
            table: "SiemSourceHealthSnapshots",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastSuccessfulIngestAtUtc",
            table: "SiemSourceHealthSnapshots",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "QueueBacklog",
            table: "SiemSourceHealthSnapshots",
            type: "integer",
            nullable: false,
            defaultValue: 0);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AverageIngestLatencyMs", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "EventsFailedLast15Minutes", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "EventsParsedLast15Minutes", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "EventsReceivedLast15Minutes", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "LastError", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "LastPollAtUtc", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "LastSuccessfulIngestAtUtc", table: "SiemSourceHealthSnapshots");
        migrationBuilder.DropColumn(name: "QueueBacklog", table: "SiemSourceHealthSnapshots");
    }
}
