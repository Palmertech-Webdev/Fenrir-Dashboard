using System;
using Fenrir.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations;

[DbContext(typeof(FenrirDbContext))]
[Migration("20260501200000_AddCaseInvestigationWorkbench")]
public partial class AddCaseInvestigationWorkbench : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Cases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Status = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                AssignedTo = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                CreatedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ClosedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Summary = table.Column<string>(type: "text", nullable: true),
                Conclusion = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CaseNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                Author = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                Note = table.Column<string>(type: "text", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseNotes", x => x.Id);
                table.ForeignKey("FK_CaseNotes_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CaseEvidence",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                EvidenceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                FileName = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                ContentType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                StorageReference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                UploadedBy = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseEvidence", x => x.Id);
                table.ForeignKey("FK_CaseEvidence_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CaseEventLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseEventLinks", x => x.Id);
                table.ForeignKey("FK_CaseEventLinks_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CaseIndicatorLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                IndicatorId = table.Column<Guid>(type: "uuid", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseIndicatorLinks", x => x.Id);
                table.ForeignKey("FK_CaseIndicatorLinks_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CaseAssetLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                AssetReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Reason = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseAssetLinks", x => x.Id);
                table.ForeignKey("FK_CaseAssetLinks_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CaseUserLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                UserReference = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                Reason = table.Column<string>(type: "text", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseUserLinks", x => x.Id);
                table.ForeignKey("FK_CaseUserLinks_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CaseTimelineItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ItemType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                Title = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                RelatedEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseTimelineItems", x => x.Id);
                table.ForeignKey("FK_CaseTimelineItems_Cases_CaseId", x => x.CaseId, "Cases", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Cases_CaseNumber", "Cases", "CaseNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Cases_Status", "Cases", "Status");
        migrationBuilder.CreateIndex("IX_Cases_Severity", "Cases", "Severity");
        migrationBuilder.CreateIndex("IX_Cases_AssignedTo", "Cases", "AssignedTo");
        migrationBuilder.CreateIndex("IX_Cases_UpdatedAtUtc", "Cases", "UpdatedAtUtc");

        migrationBuilder.CreateIndex("IX_CaseNotes_CaseId", "CaseNotes", "CaseId");
        migrationBuilder.CreateIndex("IX_CaseEvidence_CaseId", "CaseEvidence", "CaseId");
        migrationBuilder.CreateIndex("IX_CaseEventLinks_CaseId_EventId", "CaseEventLinks", new[] { "CaseId", "EventId" }, unique: true);
        migrationBuilder.CreateIndex("IX_CaseEventLinks_EventId", "CaseEventLinks", "EventId");
        migrationBuilder.CreateIndex("IX_CaseIndicatorLinks_CaseId_IndicatorId", "CaseIndicatorLinks", new[] { "CaseId", "IndicatorId" }, unique: true);
        migrationBuilder.CreateIndex("IX_CaseIndicatorLinks_IndicatorId", "CaseIndicatorLinks", "IndicatorId");
        migrationBuilder.CreateIndex("IX_CaseAssetLinks_CaseId", "CaseAssetLinks", "CaseId");
        migrationBuilder.CreateIndex("IX_CaseAssetLinks_AssetReference", "CaseAssetLinks", "AssetReference");
        migrationBuilder.CreateIndex("IX_CaseUserLinks_CaseId", "CaseUserLinks", "CaseId");
        migrationBuilder.CreateIndex("IX_CaseUserLinks_UserReference", "CaseUserLinks", "UserReference");
        migrationBuilder.CreateIndex("IX_CaseTimelineItems_CaseId_OccurredAtUtc", "CaseTimelineItems", new[] { "CaseId", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CaseTimelineItems");
        migrationBuilder.DropTable("CaseUserLinks");
        migrationBuilder.DropTable("CaseAssetLinks");
        migrationBuilder.DropTable("CaseIndicatorLinks");
        migrationBuilder.DropTable("CaseEventLinks");
        migrationBuilder.DropTable("CaseEvidence");
        migrationBuilder.DropTable("CaseNotes");
        migrationBuilder.DropTable("Cases");
    }
}
