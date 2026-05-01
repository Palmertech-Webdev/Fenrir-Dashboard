using System.Text.Json;

namespace Fenrir.Application.Siem.Parsing;

public sealed class GenericJsonSiemParser : ISiemParser
{
    public const string Name = "generic_json_v1";
    public string ParserName => Name;

    public bool CanParse(SiemRawEventInput input) => input.RawJson.HasValue || !string.IsNullOrWhiteSpace(input.RawText);

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var raw = SiemParserHelpers.RawJsonOrDefault(input);
        var element = input.RawJson;

        if (!element.HasValue && !string.IsNullOrWhiteSpace(input.RawText))
        {
            return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
            {
                TimestampUtc = input.ReceivedAtUtc ?? DateTime.UtcNow,
                SourceId = input.SourceId,
                SourceName = input.SourceName,
                Vendor = input.Vendor,
                Product = input.Product,
                EventType = "generic_text_event",
                EventCategory = "generic",
                Host = "unknown",
                Message = input.RawText,
                RawJson = raw
            });
        }

        var json = element!.Value;
        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = SiemParserHelpers.Timestamp(json, "timestamp", "time", "event.created", "@timestamp") ?? input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor,
            Product = input.Product,
            EventType = SiemParserHelpers.String(json, "eventType", "event.type", "type", "name") ?? "generic_json_event",
            EventCategory = SiemParserHelpers.String(json, "eventCategory", "event.category", "category") ?? "generic",
            Severity = SiemParserHelpers.SeverityFromValue(SiemParserHelpers.String(json, "severity", "level", "event.severity")),
            Host = SiemParserHelpers.String(json, "host", "hostname", "host.name", "computer") ?? "unknown",
            User = SiemParserHelpers.String(json, "user", "username", "user.name", "actor"),
            SourceIp = SiemParserHelpers.String(json, "sourceIp", "src_ip", "source.ip", "client.ip"),
            DestinationIp = SiemParserHelpers.String(json, "destinationIp", "dest_ip", "destination.ip", "server.ip"),
            SourcePort = SiemParserHelpers.Int(json, "sourcePort", "src_port", "source.port"),
            DestinationPort = SiemParserHelpers.Int(json, "destinationPort", "dest_port", "destination.port"),
            Domain = SiemParserHelpers.String(json, "domain", "dns.question.name", "url.domain"),
            Url = SiemParserHelpers.String(json, "url", "url.original", "request.url"),
            FileName = SiemParserHelpers.String(json, "fileName", "file.name"),
            FilePath = SiemParserHelpers.String(json, "filePath", "file.path"),
            FileHashSha256 = SiemParserHelpers.String(json, "sha256", "file.hash.sha256", "hash"),
            ProcessName = SiemParserHelpers.String(json, "processName", "process.name"),
            CommandLine = SiemParserHelpers.String(json, "commandLine", "process.command_line"),
            ParentProcessName = SiemParserHelpers.String(json, "parentProcessName", "process.parent.name"),
            Mailbox = SiemParserHelpers.String(json, "mailbox", "mailbox.owner", "recipient"),
            CloudTenantId = SiemParserHelpers.String(json, "tenantId", "cloud.tenant.id"),
            CloudResourceId = SiemParserHelpers.String(json, "resourceId", "cloud.resource.id"),
            Action = SiemParserHelpers.String(json, "action", "event.action", "operation"),
            Outcome = SiemParserHelpers.String(json, "outcome", "event.outcome", "result"),
            Message = SiemParserHelpers.String(json, "message", "msg", "summary") ?? raw,
            RawJson = raw
        });
    }
}

public sealed class ZeekJsonSiemParser : ISiemParser
{
    public string ParserName => "zeek_json_v1";

    public bool CanParse(SiemRawEventInput input) =>
        input.RawJson.HasValue && (
            SiemParserHelpers.String(input.RawJson.Value, "uid") is not null ||
            SiemParserHelpers.String(input.RawJson.Value, "id.orig_h") is not null ||
            SiemParserHelpers.String(input.RawJson.Value, "id.resp_h") is not null);

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.RawJson.HasValue) return Task.FromResult<SiemParsedEvent?>(null);
        var json = input.RawJson.Value;
        var query = SiemParserHelpers.String(json, "query");
        var uri = SiemParserHelpers.String(json, "uri");
        var method = SiemParserHelpers.String(json, "method");

        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = SiemParserHelpers.Timestamp(json, "ts", "timestamp") ?? input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor ?? "Zeek",
            Product = input.Product ?? "Zeek",
            EventType = method is not null ? "zeek_http" : query is not null ? "zeek_dns" : "zeek_connection",
            EventCategory = method is not null ? "web" : query is not null ? "dns" : "network",
            Severity = "Low",
            Host = SiemParserHelpers.String(json, "host", "id.orig_h") ?? "unknown",
            SourceIp = SiemParserHelpers.String(json, "id.orig_h", "src_ip"),
            DestinationIp = SiemParserHelpers.String(json, "id.resp_h", "dest_ip"),
            SourcePort = SiemParserHelpers.Int(json, "id.orig_p", "src_port"),
            DestinationPort = SiemParserHelpers.Int(json, "id.resp_p", "dest_port"),
            Domain = query ?? SiemParserHelpers.String(json, "host"),
            Url = uri,
            Action = method ?? SiemParserHelpers.String(json, "proto"),
            Outcome = SiemParserHelpers.String(json, "status_code", "rcode_name"),
            Message = SiemParserHelpers.String(json, "status_msg") ?? $"Zeek event {SiemParserHelpers.String(json, "uid") ?? string.Empty}".Trim(),
            RawJson = SiemParserHelpers.RawJsonOrDefault(input)
        });
    }
}

public sealed class SuricataEveJsonSiemParser : ISiemParser
{
    public string ParserName => "suricata_eve_json_v1";

    public bool CanParse(SiemRawEventInput input) =>
        input.RawJson.HasValue && SiemParserHelpers.String(input.RawJson.Value, "event_type") is not null;

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.RawJson.HasValue) return Task.FromResult<SiemParsedEvent?>(null);
        var json = input.RawJson.Value;
        var eventType = SiemParserHelpers.String(json, "event_type") ?? "suricata_event";
        var signature = SiemParserHelpers.String(json, "alert.signature");
        var severityNumber = SiemParserHelpers.Int(json, "alert.severity");

        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = SiemParserHelpers.Timestamp(json, "timestamp") ?? input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor ?? "OISF",
            Product = input.Product ?? "Suricata",
            EventType = $"suricata_{eventType}",
            EventCategory = eventType == "alert" ? "ids_alert" : "network",
            Severity = severityNumber switch { 1 => "Critical", 2 => "High", 3 => "Medium", _ => "Low" },
            Host = SiemParserHelpers.String(json, "src_ip") ?? "unknown",
            SourceIp = SiemParserHelpers.String(json, "src_ip"),
            DestinationIp = SiemParserHelpers.String(json, "dest_ip"),
            SourcePort = SiemParserHelpers.Int(json, "src_port"),
            DestinationPort = SiemParserHelpers.Int(json, "dest_port"),
            Domain = SiemParserHelpers.String(json, "dns.rrname", "http.hostname", "tls.sni"),
            Url = SiemParserHelpers.String(json, "http.url"),
            Action = SiemParserHelpers.String(json, "alert.action", "app_proto"),
            Outcome = SiemParserHelpers.String(json, "flow.state"),
            Message = signature ?? $"Suricata {eventType} event",
            RawJson = SiemParserHelpers.RawJsonOrDefault(input)
        });
    }
}

public sealed class M365AuditSiemParser : ISiemParser
{
    public string ParserName => "m365_audit_v1";

    public bool CanParse(SiemRawEventInput input) =>
        input.RawJson.HasValue && (SiemParserHelpers.String(input.RawJson.Value, "Operation") is not null || SiemParserHelpers.String(input.RawJson.Value, "Workload") is not null);

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.RawJson.HasValue) return Task.FromResult<SiemParsedEvent?>(null);
        var json = input.RawJson.Value;
        var workload = SiemParserHelpers.String(json, "Workload") ?? "Microsoft365";
        var operation = SiemParserHelpers.String(json, "Operation") ?? "m365_audit_event";

        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = SiemParserHelpers.Timestamp(json, "CreationTime", "TimeGenerated") ?? input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor ?? "Microsoft",
            Product = input.Product ?? "Microsoft 365",
            EventType = operation,
            EventCategory = workload.Contains("Exchange", StringComparison.OrdinalIgnoreCase) ? "email" : "cloud_identity",
            Severity = "Low",
            Host = SiemParserHelpers.String(json, "ClientIP") ?? "cloud",
            User = SiemParserHelpers.String(json, "UserId", "UserKey", "Actor.UserId"),
            SourceIp = SiemParserHelpers.String(json, "ClientIP", "ClientIPAddress"),
            Domain = SiemParserHelpers.String(json, "ObjectId"),
            Mailbox = SiemParserHelpers.String(json, "MailboxOwnerUPN", "UserId"),
            CloudTenantId = SiemParserHelpers.String(json, "OrganizationId", "TenantId"),
            CloudResourceId = SiemParserHelpers.String(json, "ObjectId", "ItemName"),
            Action = operation,
            Outcome = SiemParserHelpers.String(json, "ResultStatus", "Result"),
            Message = $"M365 {workload} operation: {operation}",
            RawJson = SiemParserHelpers.RawJsonOrDefault(input)
        });
    }
}

public sealed class AwsCloudTrailSiemParser : ISiemParser
{
    public string ParserName => "aws_cloudtrail_v1";

    public bool CanParse(SiemRawEventInput input) =>
        input.RawJson.HasValue && SiemParserHelpers.String(input.RawJson.Value, "eventSource") is not null;

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.RawJson.HasValue) return Task.FromResult<SiemParsedEvent?>(null);
        var json = input.RawJson.Value;
        var eventName = SiemParserHelpers.String(json, "eventName") ?? "cloudtrail_event";
        var error = SiemParserHelpers.String(json, "errorCode");

        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = SiemParserHelpers.Timestamp(json, "eventTime") ?? input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor ?? "AWS",
            Product = input.Product ?? "CloudTrail",
            EventType = eventName,
            EventCategory = "cloud_control_plane",
            Severity = error is null ? "Low" : "Medium",
            Host = SiemParserHelpers.String(json, "sourceIPAddress") ?? "aws",
            User = SiemParserHelpers.String(json, "userIdentity.arn", "userIdentity.userName", "userIdentity.principalId"),
            SourceIp = SiemParserHelpers.String(json, "sourceIPAddress"),
            CloudTenantId = SiemParserHelpers.String(json, "recipientAccountId", "userIdentity.accountId"),
            CloudResourceId = SiemParserHelpers.String(json, "resources.0.ARN", "requestParameters.bucketName"),
            Action = eventName,
            Outcome = error is null ? "success" : "failure",
            Message = error is null ? $"AWS CloudTrail action: {eventName}" : $"AWS CloudTrail action failed: {eventName} ({error})",
            RawJson = SiemParserHelpers.RawJsonOrDefault(input)
        });
    }
}

public sealed class WindowsEventJsonSiemParser : ISiemParser
{
    public string ParserName => "windows_event_json_v1";

    public bool CanParse(SiemRawEventInput input) =>
        input.RawJson.HasValue && (SiemParserHelpers.String(input.RawJson.Value, "EventID") is not null || SiemParserHelpers.String(input.RawJson.Value, "ProviderName") is not null);

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!input.RawJson.HasValue) return Task.FromResult<SiemParsedEvent?>(null);
        var json = input.RawJson.Value;
        var eventId = SiemParserHelpers.String(json, "EventID", "event_id") ?? "windows_event";
        var provider = SiemParserHelpers.String(json, "ProviderName", "provider") ?? "Windows";

        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = SiemParserHelpers.Timestamp(json, "TimeCreated", "timestamp") ?? input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor ?? "Microsoft",
            Product = input.Product ?? "Windows Event Log",
            EventType = $"windows_{eventId}",
            EventCategory = "endpoint",
            Severity = SiemParserHelpers.SeverityFromValue(SiemParserHelpers.String(json, "LevelDisplayName", "level")),
            Host = SiemParserHelpers.String(json, "MachineName", "Computer", "host") ?? "unknown",
            User = SiemParserHelpers.String(json, "TargetUserName", "SubjectUserName", "User"),
            SourceIp = SiemParserHelpers.String(json, "IpAddress", "SourceNetworkAddress"),
            ProcessName = SiemParserHelpers.String(json, "ProcessName", "NewProcessName"),
            CommandLine = SiemParserHelpers.String(json, "CommandLine", "ProcessCommandLine"),
            ParentProcessName = SiemParserHelpers.String(json, "ParentProcessName"),
            Action = provider,
            Outcome = SiemParserHelpers.String(json, "Status", "Result"),
            Message = SiemParserHelpers.String(json, "Message") ?? $"Windows event {eventId}",
            RawJson = SiemParserHelpers.RawJsonOrDefault(input)
        });
    }
}

public sealed class SyslogBasicSiemParser : ISiemParser
{
    public string ParserName => "syslog_basic_v1";

    public bool CanParse(SiemRawEventInput input) => !string.IsNullOrWhiteSpace(input.RawText);

    public Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var text = input.RawText ?? string.Empty;
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var host = parts.Length >= 4 ? parts[3] : "unknown";

        return Task.FromResult<SiemParsedEvent?>(new SiemParsedEvent
        {
            TimestampUtc = input.ReceivedAtUtc ?? DateTime.UtcNow,
            SourceId = input.SourceId,
            SourceName = input.SourceName,
            Vendor = input.Vendor,
            Product = input.Product ?? "Syslog",
            EventType = "syslog_event",
            EventCategory = "network_or_system",
            Severity = SiemParserHelpers.SeverityFromValue(text.Contains("error", StringComparison.OrdinalIgnoreCase) ? "error" : "info"),
            Host = host,
            Message = text,
            RawJson = SiemParserHelpers.RawJsonOrDefault(input)
        });
    }
}
