using Fenrir.Application.Abstractions;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fenrir.Infrastructure.Database;

public sealed class EfFenrirDataStore(FenrirDbContext dbContext) : IFenrirDataStore
{
    public async Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddFindingAsync(Finding finding, CancellationToken cancellationToken)
    {
        dbContext.Findings.Add(finding);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Finding>> ListFindingsAsync(CancellationToken cancellationToken) =>
        await dbContext.Findings.OrderByDescending(finding => finding.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<Finding?> GetFindingAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Findings.FindAsync([id], cancellationToken);

    public async Task UpdateFindingAsync(Finding finding, CancellationToken cancellationToken)
    {
        dbContext.Findings.Update(finding);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEmailCheckAsync(EmailCheck check, CancellationToken cancellationToken)
    {
        dbContext.EmailChecks.Add(check);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEmailHeaderCheckAsync(EmailHeaderCheck check, CancellationToken cancellationToken)
    {
        dbContext.EmailHeaderChecks.Add(check);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Indicator?> FindIndicatorAsync(string normalizedIndicator, CancellationToken cancellationToken) =>
        await dbContext.Indicators.FirstOrDefaultAsync(indicator => indicator.NormalizedValue == normalizedIndicator, cancellationToken);

    public async Task<IReadOnlyList<Indicator>> FindIndicatorsAsync(IEnumerable<string> normalizedIndicators, CancellationToken cancellationToken)
    {
        var values = normalizedIndicators.ToArray();
        return await dbContext.Indicators.Where(indicator => values.Contains(indicator.NormalizedValue)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Indicator>> ListIndicatorsAsync(CancellationToken cancellationToken) =>
        await dbContext.Indicators.OrderBy(indicator => indicator.Type).ThenBy(indicator => indicator.NormalizedValue).ToListAsync(cancellationToken);

    public async Task UpsertIndicatorsAsync(IEnumerable<Indicator> indicators, CancellationToken cancellationToken)
    {
        foreach (var indicator in indicators)
        {
            var existing = await dbContext.Indicators.FirstOrDefaultAsync(current => current.NormalizedValue == indicator.NormalizedValue, cancellationToken);
            if (existing is null)
            {
                dbContext.Indicators.Add(indicator);
                continue;
            }

            existing.IndicatorValue = indicator.IndicatorValue;
            existing.Type = indicator.Type;
            existing.Verdict = indicator.Verdict;
            existing.Severity = indicator.Severity;
            existing.Confidence = indicator.Confidence;
            existing.Source = indicator.Source;
            existing.Tags = indicator.Tags;
            existing.FirstSeenUtc = indicator.FirstSeenUtc < existing.FirstSeenUtc ? indicator.FirstSeenUtc : existing.FirstSeenUtc;
            existing.LastSeenUtc = indicator.LastSeenUtc > existing.LastSeenUtc ? indicator.LastSeenUtc : DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDnsCheckAsync(DnsCheck check, CancellationToken cancellationToken)
    {
        dbContext.DnsChecks.Add(check);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<DnsCheck?> GetLatestDnsCheckAsync(string domain, CancellationToken cancellationToken) =>
        await dbContext.DnsChecks
            .Where(check => check.Domain == domain)
            .OrderByDescending(check => check.CheckedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddMonitoredDomainAsync(DnsMonitoredDomain domain, CancellationToken cancellationToken)
    {
        dbContext.DnsMonitoredDomains.Add(domain);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DnsMonitoredDomain>> ListMonitoredDomainsAsync(CancellationToken cancellationToken) =>
        await dbContext.DnsMonitoredDomains.OrderBy(domain => domain.Domain).ToListAsync(cancellationToken);

    public async Task AddDarkWebCheckAsync(DarkWebCheck check, CancellationToken cancellationToken)
    {
        dbContext.DarkWebChecks.Add(check);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddNetworkScanAsync(NetworkScan scan, CancellationToken cancellationToken)
    {
        dbContext.NetworkScans.Add(scan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNetworkScanAsync(NetworkScan scan, CancellationToken cancellationToken)
    {
        dbContext.NetworkScans.Update(scan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<NetworkScan?> GetNetworkScanAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.NetworkScans.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<NetworkScanResult>> GetNetworkScanResultsAsync(Guid scanId, CancellationToken cancellationToken) =>
        await dbContext.NetworkScanResults
            .Where(result => result.NetworkScanId == scanId)
            .OrderBy(result => result.Asset)
            .ThenBy(result => result.Port)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NetworkScanResult>> GetPreviousOpenNetworkScanResultsAsync(string target, Guid currentScanId, CancellationToken cancellationToken)
    {
        var previousScanId = await dbContext.NetworkScans
            .Where(scan => scan.Target == target && scan.Id != currentScanId && scan.Status == JobStatus.Completed)
            .OrderByDescending(scan => scan.CompletedAtUtc ?? scan.CreatedAtUtc)
            .Select(scan => scan.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousScanId == Guid.Empty)
        {
            return [];
        }

        return await dbContext.NetworkScanResults
            .Where(result => result.NetworkScanId == previousScanId && result.IsOpen)
            .ToListAsync(cancellationToken);
    }

    public async Task AddNetworkScanResultsAsync(IEnumerable<NetworkScanResult> results, CancellationToken cancellationToken)
    {
        dbContext.NetworkScanResults.AddRange(results);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        dbContext.SiemEvents.Add(securityEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSecurityEventsAsync(IEnumerable<SecurityEvent> securityEvents, CancellationToken cancellationToken)
    {
        dbContext.SiemEvents.AddRange(securityEvents);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEvent>> ListSecurityEventsAsync(string? source, string? host, string? severity, CancellationToken cancellationToken)
    {
        var query = dbContext.SiemEvents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(securityEvent => securityEvent.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(host))
        {
            query = query.Where(securityEvent => securityEvent.Host == host);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(securityEvent => securityEvent.Severity == severity);
        }

        return await query
            .OrderByDescending(securityEvent => securityEvent.TimestampUtc)
            .Take(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEvent>> SearchSecurityEventsAsync(string? source, string? host, string? severity, string? eventType, string? userName, string? ipAddress, string? indicator, DateTime? fromUtc, DateTime? toUtc, int take, CancellationToken cancellationToken)
    {
        var query = dbContext.SiemEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(source))
        {
            query = query.Where(securityEvent => securityEvent.Source == source);
        }

        if (!string.IsNullOrWhiteSpace(host))
        {
            query = query.Where(securityEvent => securityEvent.Host == host);
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query = query.Where(securityEvent => securityEvent.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(securityEvent => securityEvent.EventType == eventType);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(securityEvent => securityEvent.TimestampUtc >= fromUtc.Value.ToUniversalTime());
        }

        if (toUtc.HasValue)
        {
            query = query.Where(securityEvent => securityEvent.TimestampUtc <= toUtc.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            query = query.Where(securityEvent => securityEvent.Message.Contains(userName) || securityEvent.RawJson.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            query = query.Where(securityEvent => securityEvent.Message.Contains(ipAddress) || securityEvent.RawJson.Contains(ipAddress));
        }

        if (!string.IsNullOrWhiteSpace(indicator))
        {
            query = query.Where(securityEvent => securityEvent.Message.Contains(indicator) || securityEvent.RawJson.Contains(indicator));
        }

        return await query
            .OrderByDescending(securityEvent => securityEvent.TimestampUtc)
            .Take(Math.Clamp(take, 1, 1000))
            .ToListAsync(cancellationToken);
    }

    public async Task AddSiemLogSourceAsync(SiemLogSource source, CancellationToken cancellationToken)
    {
        dbContext.SiemLogSources.Add(source);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSiemLogSourceAsync(SiemLogSource source, CancellationToken cancellationToken)
    {
        dbContext.SiemLogSources.Update(source);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SiemLogSource?> GetSiemLogSourceAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.SiemLogSources.FindAsync([id], cancellationToken);

    public async Task<SiemLogSource?> GetSiemLogSourceByNameAsync(string name, CancellationToken cancellationToken) =>
        await dbContext.SiemLogSources.FirstOrDefaultAsync(source => source.Name == name, cancellationToken);

    public async Task<IReadOnlyList<SiemLogSource>> ListSiemLogSourcesAsync(CancellationToken cancellationToken) =>
        await dbContext.SiemLogSources.OrderBy(source => source.Name).ToListAsync(cancellationToken);

    public async Task AddSiemIngestionJobAsync(SiemIngestionJob job, CancellationToken cancellationToken)
    {
        dbContext.SiemIngestionJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSiemIngestionJobAsync(SiemIngestionJob job, CancellationToken cancellationToken)
    {
        dbContext.SiemIngestionJobs.Update(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SiemIngestionJob?> GetSiemIngestionJobAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.SiemIngestionJobs.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<SiemIngestionJob>> ListSiemIngestionJobsAsync(CancellationToken cancellationToken) =>
        await dbContext.SiemIngestionJobs.OrderByDescending(job => job.StartedAtUtc).Take(500).ToListAsync(cancellationToken);

    public async Task AddJobAsync(JobRecord job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<JobRecord?> GetJobAsync(Guid id, CancellationToken cancellationToken) =>
        await dbContext.Jobs.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<JobRecord>> ListJobsAsync(CancellationToken cancellationToken) =>
        await dbContext.Jobs.OrderByDescending(job => job.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);

    public async Task UpdateJobAsync(JobRecord job, CancellationToken cancellationToken)
    {
        dbContext.Jobs.Update(job);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
