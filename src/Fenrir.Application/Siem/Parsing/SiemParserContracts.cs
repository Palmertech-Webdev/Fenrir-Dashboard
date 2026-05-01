using System.Text.Json;

namespace Fenrir.Application.Siem.Parsing;

public sealed record SiemRawEventInput(
    string ParserName,
    Guid? SourceId,
    string SourceName,
    string? Vendor,
    string? Product,
    JsonElement? RawJson,
    string? RawText,
    DateTime? ReceivedAtUtc = null);

public sealed record SiemParsedEvent
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public Guid? SourceId { get; init; }
    public string SourceName { get; init; } = string.Empty;
    public string? Vendor { get; init; }
    public string? Product { get; init; }
    public string EventType { get; init; } = "generic_event";
    public string EventCategory { get; init; } = "generic";
    public string Severity { get; init; } = "Low";
    public string Host { get; init; } = "unknown";
    public string? User { get; init; }
    public string? SourceIp { get; init; }
    public string? DestinationIp { get; init; }
    public int? SourcePort { get; init; }
    public int? DestinationPort { get; init; }
    public string? Domain { get; init; }
    public string? Url { get; init; }
    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public string? FileHashSha256 { get; init; }
    public string? ProcessName { get; init; }
    public string? CommandLine { get; init; }
    public string? ParentProcessName { get; init; }
    public string? Mailbox { get; init; }
    public string? CloudTenantId { get; init; }
    public string? CloudResourceId { get; init; }
    public string? Action { get; init; }
    public string? Outcome { get; init; }
    public string RawJson { get; init; } = "{}";
    public string Message { get; init; } = string.Empty;
}

public interface ISiemParser
{
    string ParserName { get; }

    bool CanParse(SiemRawEventInput input);

    Task<SiemParsedEvent?> ParseAsync(SiemRawEventInput input, CancellationToken cancellationToken);
}

public interface ISiemParserRegistry
{
    ISiemParser GetParser(string parserName);
    IReadOnlyCollection<ISiemParser> ListParsers();
}
