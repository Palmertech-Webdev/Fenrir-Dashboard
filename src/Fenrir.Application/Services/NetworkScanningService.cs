using System.Net;
using System.Net.Sockets;
using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed class NetworkScanningService(IFenrirDataStore dataStore, IBackgroundJobScheduler scheduler) : INetworkScanningService
{
    private static readonly int[] QuickPorts = [21, 22, 23, 25, 53, 80, 110, 135, 139, 143, 443, 445, 465, 587, 993, 995, 1433, 3306, 3389, 5432];

    public async Task<NetworkScanCreatedResponse> CreateScanAsync(NetworkScanRequest request, CancellationToken cancellationToken)
    {
        var target = request.Target.Trim();
        ValidateTarget(target);
        var ports = BuildPortList(request.ScanType, request.Ports);

        var scan = new NetworkScan
        {
            Target = target,
            ScanType = string.IsNullOrWhiteSpace(request.ScanType) ? NetworkScanTypes.Quick : request.ScanType.Trim(),
            Ports = ports.ToList(),
            Status = JobStatus.Queued
        };

        var job = new JobRecord
        {
            JobType = "NetworkScan",
            Status = JobStatus.Queued,
            RelatedEntityId = scan.Id,
            RelatedEntityType = nameof(NetworkScan)
        };

        await dataStore.AddNetworkScanAsync(scan, cancellationToken);
        await dataStore.AddJobAsync(job, cancellationToken);
        await scheduler.ScheduleNetworkScanAsync(scan.Id, job.Id, cancellationToken);

        return new NetworkScanCreatedResponse(scan.Id, job.Id, scan.Status);
    }

    public async Task<NetworkScanDto?> GetScanAsync(Guid id, CancellationToken cancellationToken)
    {
        var scan = await dataStore.GetNetworkScanAsync(id, cancellationToken);
        if (scan is null)
        {
            return null;
        }

        var results = await dataStore.GetNetworkScanResultsAsync(id, cancellationToken);
        return scan.ToDto(results);
    }

    private static IReadOnlyList<int> BuildPortList(string? scanType, IReadOnlyList<int>? requestedPorts)
    {
        var type = string.IsNullOrWhiteSpace(scanType) ? NetworkScanTypes.Quick : scanType.Trim();
        var ports = string.Equals(type, NetworkScanTypes.Quick, StringComparison.OrdinalIgnoreCase)
            ? QuickPorts
            : requestedPorts?.ToArray() ?? QuickPorts;

        var normalized = ports
            .Where(port => port is >= 1 and <= 65535)
            .Distinct()
            .Order()
            .ToArray();

        if (normalized.Length == 0)
        {
            throw new ArgumentException("At least one valid TCP port is required.");
        }

        if (normalized.Length > 100)
        {
            throw new ArgumentException("MVP scans are limited to 100 TCP ports.");
        }

        return normalized;
    }

    private static void ValidateTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new ArgumentException("Scan target is required.");
        }

        _ = NetworkTargetExpander.Expand(target);
    }
}

public sealed class NetworkScanExecutor(IFenrirDataStore dataStore, INetworkProbe probe) : INetworkScanExecutor
{
    public async Task ExecuteAsync(Guid scanId, Guid? jobRecordId, CancellationToken cancellationToken)
    {
        var scan = await dataStore.GetNetworkScanAsync(scanId, cancellationToken);
        if (scan is null)
        {
            return;
        }

        JobRecord? job = null;
        if (jobRecordId is not null)
        {
            job = await dataStore.GetJobAsync(jobRecordId.Value, cancellationToken);
        }

        try
        {
            scan.Status = JobStatus.Running;
            scan.StartedAtUtc = DateTime.UtcNow;
            await dataStore.UpdateNetworkScanAsync(scan, cancellationToken);

            if (job is not null)
            {
                job.Status = JobStatus.Running;
                job.StartedAtUtc = scan.StartedAtUtc;
                await dataStore.UpdateJobAsync(job, cancellationToken);
            }

            var targets = NetworkTargetExpander.Expand(scan.Target);
            var results = new List<NetworkScanResult>();
            foreach (var target in targets)
            {
                foreach (var port in scan.Ports)
                {
                    var probeResult = await probe.ProbeAsync(target, port, TimeSpan.FromSeconds(2), cancellationToken);
                    var result = new NetworkScanResult
                    {
                        NetworkScanId = scan.Id,
                        Asset = target,
                        Port = port,
                        IsOpen = probeResult.IsOpen,
                        Banner = probeResult.Banner,
                        Service = KnownPortService(port),
                        Severity = probeResult.IsOpen ? PortSeverity(port) : FindingSeverity.Informational
                    };
                    results.Add(result);
                }
            }

            await dataStore.AddNetworkScanResultsAsync(results, cancellationToken);
            await CreateNewExposureFindingsAsync(scan, results, cancellationToken);

            scan.Status = JobStatus.Completed;
            scan.CompletedAtUtc = DateTime.UtcNow;
            await dataStore.UpdateNetworkScanAsync(scan, cancellationToken);

            if (job is not null)
            {
                job.Status = JobStatus.Completed;
                job.CompletedAtUtc = scan.CompletedAtUtc;
                await dataStore.UpdateJobAsync(job, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            scan.Status = JobStatus.Failed;
            scan.Error = exception.Message;
            scan.CompletedAtUtc = DateTime.UtcNow;
            await dataStore.UpdateNetworkScanAsync(scan, CancellationToken.None);

            if (job is not null)
            {
                job.Status = JobStatus.Failed;
                job.Error = exception.Message;
                job.CompletedAtUtc = scan.CompletedAtUtc;
                await dataStore.UpdateJobAsync(job, CancellationToken.None);
            }
        }
    }

    private async Task CreateNewExposureFindingsAsync(NetworkScan scan, IReadOnlyList<NetworkScanResult> results, CancellationToken cancellationToken)
    {
        var previousOpen = await dataStore.GetPreviousOpenNetworkScanResultsAsync(scan.Target, scan.Id, cancellationToken);
        var previousKeys = previousOpen.Select(result => $"{result.Asset}:{result.Port}").ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var result in results.Where(result => result.IsOpen))
        {
            var key = $"{result.Asset}:{result.Port}";
            if (previousKeys.Contains(key))
            {
                continue;
            }

            var finding = new Finding
            {
                Module = "NetworkScanning",
                Type = "NetworkFinding",
                Severity = result.Severity,
                RiskScore = result.Severity switch
                {
                    FindingSeverity.High => 75,
                    FindingSeverity.Medium => 50,
                    _ => 20
                },
                Title = $"New exposed {result.Service ?? "TCP"} service detected",
                Summary = $"{result.Asset}:{result.Port} is reachable.",
                Evidence = $"Asset={result.Asset}; Port={result.Port}; Banner={result.Banner ?? "none"}",
                Recommendation = "Confirm whether the exposure is expected and restrict access where possible.",
                RelatedEntityId = scan.Id,
                RelatedEntityType = nameof(NetworkScan)
            };
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }
    }

    private static string? KnownPortService(int port) => port switch
    {
        21 => "FTP",
        22 => "SSH",
        23 => "Telnet",
        25 => "SMTP",
        53 => "DNS",
        80 => "HTTP",
        110 => "POP3",
        135 => "MSRPC",
        139 => "NetBIOS",
        143 => "IMAP",
        443 => "HTTPS",
        445 => "SMB",
        465 => "SMTPS",
        587 => "SMTP Submission",
        993 => "IMAPS",
        995 => "POP3S",
        1433 => "SQL Server",
        3306 => "MySQL",
        3389 => "RDP",
        5432 => "PostgreSQL",
        _ => null
    };

    private static string PortSeverity(int port) => port switch
    {
        23 or 445 or 3389 => FindingSeverity.High,
        21 or 22 or 135 or 139 or 1433 or 3306 or 5432 => FindingSeverity.Medium,
        _ => FindingSeverity.Low
    };
}

internal static class NetworkTargetExpander
{
    public static IReadOnlyList<string> Expand(string target)
    {
        target = target.Trim();
        if (IPAddress.TryParse(target, out var singleIp))
        {
            return [singleIp.ToString()];
        }

        if (target.Contains('/', StringComparison.Ordinal))
        {
            return ExpandCidr(target);
        }

        if (target.Contains('-', StringComparison.Ordinal))
        {
            return ExpandRange(target);
        }

        if (Uri.CheckHostName(target) is UriHostNameType.Dns)
        {
            return [target];
        }

        throw new ArgumentException("Target must be an IP address, hostname, CIDR, or IPv4 range.");
    }

    private static IReadOnlyList<string> ExpandCidr(string cidr)
    {
        var parts = cidr.Split('/', 2);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var network) || !int.TryParse(parts[1], out var prefix))
        {
            throw new ArgumentException("CIDR target is invalid.");
        }

        if (network.AddressFamily != AddressFamily.InterNetwork || prefix is < 24 or > 32)
        {
            throw new ArgumentException("MVP CIDR scans are limited to IPv4 /24 through /32 ranges.");
        }

        var mask = uint.MaxValue << (32 - prefix);
        var networkValue = ToUInt32(network) & mask;
        var hostCount = 1u << (32 - prefix);
        if (hostCount > 256)
        {
            throw new ArgumentException("MVP network scans are limited to 256 hosts.");
        }

        var addresses = new List<string>();
        for (var i = 0u; i < hostCount; i++)
        {
            addresses.Add(FromUInt32(networkValue + i).ToString());
        }

        return addresses;
    }

    private static IReadOnlyList<string> ExpandRange(string range)
    {
        var parts = range.Split('-', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var start) || !IPAddress.TryParse(parts[1], out var end))
        {
            throw new ArgumentException("IPv4 range target is invalid.");
        }

        if (start.AddressFamily != AddressFamily.InterNetwork || end.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("MVP range scans support IPv4 only.");
        }

        var startValue = ToUInt32(start);
        var endValue = ToUInt32(end);
        if (endValue < startValue || endValue - startValue + 1 > 256)
        {
            throw new ArgumentException("MVP network scans are limited to 256 hosts.");
        }

        var addresses = new List<string>();
        for (var value = startValue; value <= endValue; value++)
        {
            addresses.Add(FromUInt32(value).ToString());
        }

        return addresses;
    }

    private static uint ToUInt32(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt32(bytes, 0);
    }

    private static IPAddress FromUInt32(uint value)
    {
        var bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return new IPAddress(bytes);
    }
}
