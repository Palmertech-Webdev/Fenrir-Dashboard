namespace Fenrir.Domain.Enums;

public static class FindingSeverity
{
    public const string Informational = "Informational";
    public const string Low = "Low";
    public const string Medium = "Medium";
    public const string High = "High";
    public const string Critical = "Critical";
}

public static class FindingStatus
{
    public const string Open = "Open";
    public const string Triaged = "Triaged";
    public const string InProgress = "InProgress";
    public const string Resolved = "Resolved";
    public const string Dismissed = "Dismissed";
}

public static class IndicatorTypes
{
    public const string Unknown = "Unknown";
    public const string IpAddress = "IpAddress";
    public const string Domain = "Domain";
    public const string Url = "Url";
    public const string FileHash = "FileHash";
    public const string EmailAddress = "EmailAddress";
}

public static class IndicatorVerdicts
{
    public const string Unknown = "Unknown";
    public const string Allowlisted = "Allowlisted";
    public const string Benign = "Benign";
    public const string Suspicious = "Suspicious";
    public const string Malicious = "Malicious";
}

public static class JobStatus
{
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class NetworkScanTypes
{
    public const string Quick = "Quick";
    public const string Standard = "Standard";
    public const string Custom = "Custom";
}
