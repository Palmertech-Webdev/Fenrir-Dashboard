using Fenrir.Application.Abstractions;
using Fenrir.Application.Services;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Tests;

public sealed class ModuleServiceTests
{
    [Fact]
    public void IndicatorClassifier_Normalizes_Core_Ioc_Types()
    {
        Assert.Equal(IndicatorTypes.IpAddress, IndicatorClassifier.Classify("192.168.1.10").Type);
        Assert.Equal(IndicatorTypes.Url, IndicatorClassifier.Classify("https://Example.com/login").Type);
        Assert.Equal(IndicatorTypes.EmailAddress, IndicatorClassifier.Classify("User@Example.com").Type);
        Assert.Equal("user@example.com", IndicatorClassifier.Classify("User@Example.com").Normalized);
        Assert.Equal(IndicatorTypes.FileHash, IndicatorClassifier.Classify("0123456789abcdef0123456789abcdef").Type);
        Assert.Equal(IndicatorTypes.Domain, IndicatorClassifier.Classify("Example.COM").Type);
    }

    [Fact]
    public async Task EmailVerification_Flags_Missing_Dmarc()
    {
        var store = new InMemoryFenrirDataStore();
        var dns = new StubDnsLookupService
        {
            Mx = ["10 mail.example.com"],
            Txt = ["v=spf1 include:_spf.example.com -all"],
            DmarcTxt = []
        };

        var service = new EmailVerificationService(dns, store);
        var response = await service.VerifyAsync(new EmailVerificationRequest("analyst@example.com"), CancellationToken.None);

        Assert.True(response.MxPresent);
        Assert.True(response.SpfPresent);
        Assert.False(response.DmarcPresent);
        Assert.Equal(FindingSeverity.Medium, response.Risk);
        Assert.Contains(response.Findings, finding => finding.Title == "No DMARC record found");
    }

    [Fact]
    public async Task HeaderCheck_Creates_High_Finding_For_Dmarc_Failure()
    {
        var store = new InMemoryFenrirDataStore();
        var service = new EmailHeaderCheckService(store);
        const string headers = """
            From: Example <sender@example.com>
            Reply-To: Attacker <reply@evil.example>
            Return-Path: <bounce@example.com>
            Authentication-Results: mx.example.com; spf=pass smtp.mailfrom=example.com; dkim=pass header.d=example.com; dmarc=fail header.from=example.com
            Received: from mail.example.com (mail.example.com [203.0.113.25]) by mx.example.com with ESMTPS id abc123
            """;

        var response = await service.CheckAsync(new EmailHeaderCheckRequest(headers), CancellationToken.None);

        Assert.Equal("fail", response.DmarcResult);
        Assert.True(response.FromReplyToMismatch);
        Assert.Contains(response.Findings, finding => finding.Title == "DMARC failed" && finding.Severity == FindingSeverity.High);
    }

    [Fact]
    public async Task IocCheck_Creates_Finding_When_Local_Malicious_Match_Exists()
    {
        var store = new InMemoryFenrirDataStore();
        await store.UpsertIndicatorsAsync(
            [
                new Indicator
                {
                    IndicatorValue = "malicious-domain.example",
                    NormalizedValue = "malicious-domain.example",
                    Type = IndicatorTypes.Domain,
                    Verdict = IndicatorVerdicts.Malicious,
                    Severity = FindingSeverity.High,
                    Confidence = 85,
                    Source = "Manual import",
                    Tags = ["phishing", "credential-theft"]
                }
            ],
            CancellationToken.None);

        var service = new IocService(store);
        var response = await service.CheckAsync(new IocCheckRequest(Indicator: "malicious-domain.example"), CancellationToken.None);

        var result = Assert.Single(response.Results);
        Assert.True(result.Matched);
        Assert.Equal(IndicatorVerdicts.Malicious, result.Verdict);
        Assert.NotNull(result.Finding);
        Assert.Single(store.Findings);
    }
}

internal sealed class StubDnsLookupService : IDnsLookupService
{
    public IReadOnlyList<string> Mx { get; init; } = [];
    public IReadOnlyList<string> Txt { get; init; } = [];
    public IReadOnlyList<string> DmarcTxt { get; init; } = [];

    public Task<IReadOnlyList<string>> GetARecordsAsync(string domain, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetAaaaRecordsAsync(string domain, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetMxRecordsAsync(string domain, CancellationToken cancellationToken) => Task.FromResult(Mx);

    public Task<IReadOnlyList<string>> GetTxtRecordsAsync(string domain, CancellationToken cancellationToken) =>
        Task.FromResult(domain.StartsWith("_dmarc.", StringComparison.OrdinalIgnoreCase) ? DmarcTxt : Txt);

    public Task<IReadOnlyList<string>> GetNameServersAsync(string domain, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetCaaRecordsAsync(string domain, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<bool> HasDnsSecAsync(string domain, CancellationToken cancellationToken) => Task.FromResult(false);
}

internal sealed class InMemoryFenrirDataStore : IFenrirDataStore
{
    public List<Finding> Findings { get; } = [];
    public List<Indicator> Indicators { get; } = [];
    private readonly List<EmailCheck> emailChecks = [];
    private readonly List<EmailHeaderCheck> emailHeaderChecks = [];
    private readonly List<DnsCheck> dnsChecks = [];
    private readonly List<DnsMonitoredDomain> monitoredDomains = [];
    private readonly List<NetworkScan> networkScans = [];
    private readonly List<NetworkScanResult> networkScanResults = [];
    private readonly List<SecurityEvent> securityEvents = [];
    private readonly List<JobRecord> jobs = [];

    public Task AddAuditLogAsync(AuditLog auditLog, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AddFindingAsync(Finding finding, CancellationToken cancellationToken)
    {
        Findings.Add(finding);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Finding>> ListFindingsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Finding>>(Findings);

    public Task<Finding?> GetFindingAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(Findings.FirstOrDefault(finding => finding.Id == id));

    public Task UpdateFindingAsync(Finding finding, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AddEmailCheckAsync(EmailCheck check, CancellationToken cancellationToken)
    {
        emailChecks.Add(check);
        return Task.CompletedTask;
    }

    public Task AddEmailHeaderCheckAsync(EmailHeaderCheck check, CancellationToken cancellationToken)
    {
        emailHeaderChecks.Add(check);
        return Task.CompletedTask;
    }

    public Task<Indicator?> FindIndicatorAsync(string normalizedIndicator, CancellationToken cancellationToken) =>
        Task.FromResult(Indicators.FirstOrDefault(indicator => indicator.NormalizedValue == normalizedIndicator));

    public Task<IReadOnlyList<Indicator>> FindIndicatorsAsync(IEnumerable<string> normalizedIndicators, CancellationToken cancellationToken)
    {
        var values = normalizedIndicators.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyList<Indicator>>(Indicators.Where(indicator => values.Contains(indicator.NormalizedValue)).ToArray());
    }

    public Task<IReadOnlyList<Indicator>> ListIndicatorsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Indicator>>(Indicators);

    public Task UpsertIndicatorsAsync(IEnumerable<Indicator> indicators, CancellationToken cancellationToken)
    {
        foreach (var indicator in indicators)
        {
            Indicators.RemoveAll(current => current.NormalizedValue == indicator.NormalizedValue);
            Indicators.Add(indicator);
        }

        return Task.CompletedTask;
    }

    public Task AddDnsCheckAsync(DnsCheck check, CancellationToken cancellationToken)
    {
        dnsChecks.Add(check);
        return Task.CompletedTask;
    }

    public Task<DnsCheck?> GetLatestDnsCheckAsync(string domain, CancellationToken cancellationToken) =>
        Task.FromResult(dnsChecks.LastOrDefault(check => check.Domain == domain));

    public Task AddMonitoredDomainAsync(DnsMonitoredDomain domain, CancellationToken cancellationToken)
    {
        monitoredDomains.Add(domain);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DnsMonitoredDomain>> ListMonitoredDomainsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DnsMonitoredDomain>>(monitoredDomains);

    public Task AddDarkWebCheckAsync(DarkWebCheck check, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AddNetworkScanAsync(NetworkScan scan, CancellationToken cancellationToken)
    {
        networkScans.Add(scan);
        return Task.CompletedTask;
    }

    public Task UpdateNetworkScanAsync(NetworkScan scan, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<NetworkScan?> GetNetworkScanAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(networkScans.FirstOrDefault(scan => scan.Id == id));

    public Task<IReadOnlyList<NetworkScanResult>> GetNetworkScanResultsAsync(Guid scanId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<NetworkScanResult>>(networkScanResults.Where(result => result.NetworkScanId == scanId).ToArray());

    public Task<IReadOnlyList<NetworkScanResult>> GetPreviousOpenNetworkScanResultsAsync(string target, Guid currentScanId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<NetworkScanResult>>([]);

    public Task AddNetworkScanResultsAsync(IEnumerable<NetworkScanResult> results, CancellationToken cancellationToken)
    {
        networkScanResults.AddRange(results);
        return Task.CompletedTask;
    }

    public Task AddSecurityEventAsync(SecurityEvent securityEvent, CancellationToken cancellationToken)
    {
        securityEvents.Add(securityEvent);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SecurityEvent>> ListSecurityEventsAsync(string? source, string? host, string? severity, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SecurityEvent>>(securityEvents);

    public Task AddJobAsync(JobRecord job, CancellationToken cancellationToken)
    {
        jobs.Add(job);
        return Task.CompletedTask;
    }

    public Task<JobRecord?> GetJobAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(jobs.FirstOrDefault(job => job.Id == id));

    public Task<IReadOnlyList<JobRecord>> ListJobsAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<JobRecord>>(jobs);

    public Task UpdateJobAsync(JobRecord job, CancellationToken cancellationToken) => Task.CompletedTask;
}
