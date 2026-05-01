using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fenrir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiemIngestionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KeyHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    Hostname = table.Column<string>(type: "text", nullable: true),
                    Owner = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Actor = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DarkWebChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Query = table.Column<string>(type: "text", nullable: false),
                    QueryType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Exposed = table.Column<bool>(type: "boolean", nullable: false),
                    BreachCount = table.Column<int>(type: "integer", nullable: false),
                    Sources = table.Column<List<string>>(type: "text[]", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DarkWebChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DnsChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    ARecords = table.Column<List<string>>(type: "text[]", nullable: false),
                    AaaaRecords = table.Column<List<string>>(type: "text[]", nullable: false),
                    MxRecords = table.Column<List<string>>(type: "text[]", nullable: false),
                    TxtRecords = table.Column<List<string>>(type: "text[]", nullable: false),
                    CaaRecords = table.Column<List<string>>(type: "text[]", nullable: false),
                    NsRecords = table.Column<List<string>>(type: "text[]", nullable: false),
                    SpfPresent = table.Column<bool>(type: "boolean", nullable: false),
                    DmarcPresent = table.Column<bool>(type: "boolean", nullable: false),
                    DnsSecAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Risk = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DnsMonitoredDomains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Owner = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsMonitoredDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DnsObservationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Hostname = table.Column<string>(type: "text", nullable: false),
                    QueriedDomain = table.Column<string>(type: "text", nullable: false),
                    ResolvedIp = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Verdict = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MatchedIndicatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DnsObservationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    FormatValid = table.Column<bool>(type: "boolean", nullable: false),
                    MxPresent = table.Column<bool>(type: "boolean", nullable: false),
                    SpfPresent = table.Column<bool>(type: "boolean", nullable: false),
                    DmarcPresent = table.Column<bool>(type: "boolean", nullable: false),
                    DkimSelector = table.Column<string>(type: "text", nullable: true),
                    DkimPresent = table.Column<bool>(type: "boolean", nullable: true),
                    DisposableDomain = table.Column<bool>(type: "boolean", nullable: false),
                    TrustScore = table.Column<int>(type: "integer", nullable: false),
                    Risk = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailHeaderChecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    From = table.Column<string>(type: "text", nullable: false),
                    ReplyTo = table.Column<string>(type: "text", nullable: true),
                    ReturnPath = table.Column<string>(type: "text", nullable: true),
                    ReceivedChain = table.Column<List<string>>(type: "text[]", nullable: false),
                    SendingIps = table.Column<List<string>>(type: "text[]", nullable: false),
                    SpfResult = table.Column<string>(type: "text", nullable: true),
                    DkimResult = table.Column<string>(type: "text", nullable: true),
                    DmarcResult = table.Column<string>(type: "text", nullable: true),
                    FromReplyToMismatch = table.Column<bool>(type: "boolean", nullable: false),
                    SuspiciousRelayChainDetected = table.Column<bool>(type: "boolean", nullable: false),
                    PrivateIpLeakDetected = table.Column<bool>(type: "boolean", nullable: false),
                    HeaderUrls = table.Column<List<string>>(type: "text[]", nullable: false),
                    HeaderDomains = table.Column<List<string>>(type: "text[]", nullable: false),
                    Risk = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailHeaderChecks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Findings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Module = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    Evidence = table.Column<string>(type: "text", nullable: false),
                    Recommendation = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Findings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Indicators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndicatorValue = table.Column<string>(type: "text", nullable: false),
                    NormalizedValue = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Verdict = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Confidence = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    FirstSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExternalJobId = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkScanResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkScanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Asset = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    IsOpen = table.Column<bool>(type: "boolean", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: true),
                    Banner = table.Column<string>(type: "text", nullable: true),
                    Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CheckedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkScanResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NetworkScans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    ScanType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Ports = table.Column<List<int>>(type: "integer[]", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkScans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiemEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    Host = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    RawJson = table.Column<string>(type: "jsonb", nullable: false),
                    IngestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiemEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiemIngestionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    InputType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Parser = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    EventsReceived = table.Column<int>(type: "integer", nullable: false),
                    EventsParsed = table.Column<int>(type: "integer", nullable: false),
                    EventsFailed = table.Column<int>(type: "integer", nullable: false),
                    ErrorSummary = table.Column<string>(type: "text", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiemIngestionJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiemLogSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Vendor = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Product = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ConnectionType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Parser = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessfulIngestAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiemLogSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAtUtc", "DisplayName", "Email", "IsActive", "Role" },
                values: new object[] { new Guid("018f8df0-27ab-7b8d-b585-3fd0f7c2a001"), new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fenrir Admin", "admin@fenrir.local", true, "Admin" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_IpAddress",
                table: "Assets",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Name",
                table: "Assets",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CreatedAtUtc",
                table: "AuditLogs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_DarkWebChecks_Query",
                table: "DarkWebChecks",
                column: "Query");

            migrationBuilder.CreateIndex(
                name: "IX_DnsChecks_Domain",
                table: "DnsChecks",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_DnsMonitoredDomains_Domain",
                table: "DnsMonitoredDomains",
                column: "Domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DnsObservationEvents_QueriedDomain",
                table: "DnsObservationEvents",
                column: "QueriedDomain");

            migrationBuilder.CreateIndex(
                name: "IX_DnsObservationEvents_TimestampUtc",
                table: "DnsObservationEvents",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChecks_Domain",
                table: "EmailChecks",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_EmailHeaderChecks_Risk",
                table: "EmailHeaderChecks",
                column: "Risk");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Module",
                table: "Findings",
                column: "Module");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Severity",
                table: "Findings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Status",
                table: "Findings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_NormalizedValue",
                table: "Indicators",
                column: "NormalizedValue",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Type",
                table: "Indicators",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Indicators_Verdict",
                table: "Indicators",
                column: "Verdict");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_JobType",
                table: "Jobs",
                column: "JobType");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Status",
                table: "Jobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScanResults_NetworkScanId_Asset_Port",
                table: "NetworkScanResults",
                columns: new[] { "NetworkScanId", "Asset", "Port" });

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScans_Status",
                table: "NetworkScans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkScans_Target",
                table: "NetworkScans",
                column: "Target");

            migrationBuilder.CreateIndex(
                name: "IX_SiemEvents_EventType",
                table: "SiemEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SiemEvents_Host",
                table: "SiemEvents",
                column: "Host");

            migrationBuilder.CreateIndex(
                name: "IX_SiemEvents_Severity",
                table: "SiemEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SiemEvents_Source",
                table: "SiemEvents",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_SiemEvents_TimestampUtc",
                table: "SiemEvents",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SiemIngestionJobs_SourceId",
                table: "SiemIngestionJobs",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SiemIngestionJobs_StartedAtUtc",
                table: "SiemIngestionJobs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SiemIngestionJobs_Status",
                table: "SiemIngestionJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SiemLogSources_Name",
                table: "SiemLogSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SiemLogSources_SourceType",
                table: "SiemLogSources",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_SiemLogSources_Status",
                table: "SiemLogSources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "DarkWebChecks");

            migrationBuilder.DropTable(
                name: "DnsChecks");

            migrationBuilder.DropTable(
                name: "DnsMonitoredDomains");

            migrationBuilder.DropTable(
                name: "DnsObservationEvents");

            migrationBuilder.DropTable(
                name: "EmailChecks");

            migrationBuilder.DropTable(
                name: "EmailHeaderChecks");

            migrationBuilder.DropTable(
                name: "Findings");

            migrationBuilder.DropTable(
                name: "Indicators");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "NetworkScanResults");

            migrationBuilder.DropTable(
                name: "NetworkScans");

            migrationBuilder.DropTable(
                name: "SiemEvents");

            migrationBuilder.DropTable(
                name: "SiemIngestionJobs");

            migrationBuilder.DropTable(
                name: "SiemLogSources");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
