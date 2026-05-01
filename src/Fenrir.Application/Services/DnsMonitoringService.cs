using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed class DnsMonitoringService(IDnsLookupService dnsLookup, IFenrirDataStore dataStore) : IDnsMonitoringService
{
    private static readonly HashSet<string> SuspiciousTlds = new(StringComparer.OrdinalIgnoreCase)
    {
        "zip",
        "mov",
        "top",
        "xyz",
        "click",
        "country",
        "gq",
        "tk"
    };

    public async Task<DnsDomainCheckResponse> CheckDomainAsync(DnsDomainCheckRequest request, CancellationToken cancellationToken)
    {
        if (!IndicatorClassifier.TryNormalizeDomain(request.Domain, out var domain))
        {
            throw new ArgumentException("Domain is not valid.", nameof(request));
        }

        var previousCheck = await dataStore.GetLatestDnsCheckAsync(domain, cancellationToken);
        var aRecords = await dnsLookup.GetARecordsAsync(domain, cancellationToken);
        var aaaaRecords = await dnsLookup.GetAaaaRecordsAsync(domain, cancellationToken);
        var mxRecords = await dnsLookup.GetMxRecordsAsync(domain, cancellationToken);
        var txtRecords = await dnsLookup.GetTxtRecordsAsync(domain, cancellationToken);
        var dmarcRecords = await dnsLookup.GetTxtRecordsAsync($"_dmarc.{domain}", cancellationToken);
        var caaRecords = await dnsLookup.GetCaaRecordsAsync(domain, cancellationToken);
        var nsRecords = await dnsLookup.GetNameServersAsync(domain, cancellationToken);
        var dnsSecAvailable = await dnsLookup.HasDnsSecAsync(domain, cancellationToken);

        var spfPresent = txtRecords.Any(record => record.StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase));
        var dmarcPresent = dmarcRecords.Any(record => record.StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase));
        var findings = new List<Finding>();

        var domainIndicator = await dataStore.FindIndicatorAsync(domain, cancellationToken);
        if (domainIndicator is not null && IsFindingWorthy(domainIndicator))
        {
            findings.Add(CreateFinding("Domain matches IOC", domainIndicator.Severity, SecurityHelpers.SeverityWeight(domainIndicator.Severity), $"Domain={domain}; Source={domainIndicator.Source}", "Investigate whether the domain should be blocked or treated as hostile."));
        }

        foreach (var ip in aRecords.Concat(aaaaRecords))
        {
            var ipIndicator = await dataStore.FindIndicatorAsync(ip, cancellationToken);
            if (ipIndicator is not null && IsFindingWorthy(ipIndicator))
            {
                findings.Add(CreateFinding("Domain resolves to known-bad IP", ipIndicator.Severity, SecurityHelpers.SeverityWeight(ipIndicator.Severity), $"Domain={domain}; IP={ip}; Source={ipIndicator.Source}", "Investigate hosting, DNS, and endpoint traffic related to this domain."));
            }
        }

        var tld = domain.Split('.').Last();
        if (SuspiciousTlds.Contains(tld))
        {
            findings.Add(CreateFinding("Suspicious TLD observed", FindingSeverity.Medium, 40, domain, "Apply extra review before trusting domains on frequently abused TLDs."));
        }

        if (previousCheck is not null)
        {
            AddChangeFindings(previousCheck, mxRecords, spfPresent, dmarcPresent, findings);
        }

        var riskScore = findings.Count == 0 ? 10 : findings.Max(f => f.RiskScore);
        var risk = SecurityHelpers.SeverityFromScore(riskScore);
        var summary = findings.Count == 0
            ? "DNS posture check completed without risky changes or IOC matches."
            : $"{findings.Count} DNS risk signal(s) detected. Highest severity: {risk}.";

        var check = new DnsCheck
        {
            Domain = domain,
            ARecords = aRecords.ToList(),
            AaaaRecords = aaaaRecords.ToList(),
            MxRecords = mxRecords.ToList(),
            TxtRecords = txtRecords.ToList(),
            CaaRecords = caaRecords.ToList(),
            NsRecords = nsRecords.ToList(),
            SpfPresent = spfPresent,
            DmarcPresent = dmarcPresent,
            DnsSecAvailable = dnsSecAvailable,
            Risk = risk,
            Summary = summary
        };

        await dataStore.AddDnsCheckAsync(check, cancellationToken);
        foreach (var finding in findings)
        {
            finding.Module = "DNS";
            finding.Type = "DnsFinding";
            finding.RelatedEntityId = check.Id;
            finding.RelatedEntityType = nameof(DnsCheck);
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }

        return new DnsDomainCheckResponse(
            domain,
            aRecords,
            aaaaRecords,
            mxRecords,
            txtRecords,
            spfPresent,
            dmarcPresent,
            caaRecords,
            nsRecords,
            dnsSecAvailable,
            risk,
            summary,
            findings.Select(f => f.ToDto()).ToArray());
    }

    public async Task<MonitoredDomainDto> AddMonitoredDomainAsync(MonitoredDomainRequest request, CancellationToken cancellationToken)
    {
        if (!IndicatorClassifier.TryNormalizeDomain(request.Domain, out var domain))
        {
            throw new ArgumentException("Domain is not valid.", nameof(request));
        }

        var existing = (await dataStore.ListMonitoredDomainsAsync(cancellationToken))
            .FirstOrDefault(monitored => string.Equals(monitored.Domain, domain, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return existing.ToDto();
        }

        var monitoredDomain = new DnsMonitoredDomain
        {
            Domain = domain,
            Owner = request.Owner
        };

        await dataStore.AddMonitoredDomainAsync(monitoredDomain, cancellationToken);
        return monitoredDomain.ToDto();
    }

    public async Task<IReadOnlyList<MonitoredDomainDto>> ListMonitoredDomainsAsync(CancellationToken cancellationToken)
    {
        var monitoredDomains = await dataStore.ListMonitoredDomainsAsync(cancellationToken);
        return monitoredDomains.Select(domain => domain.ToDto()).ToArray();
    }

    private static void AddChangeFindings(DnsCheck previousCheck, IReadOnlyList<string> mxRecords, bool spfPresent, bool dmarcPresent, List<Finding> findings)
    {
        if (!SetEquals(previousCheck.MxRecords, mxRecords))
        {
            findings.Add(CreateFinding("MX record changed", FindingSeverity.Medium, 45, $"Previous={string.Join(", ", previousCheck.MxRecords)}; Current={string.Join(", ", mxRecords)}", "Confirm whether the mail-routing change is expected."));
        }

        if (previousCheck.SpfPresent && !spfPresent)
        {
            findings.Add(CreateFinding("SPF weakened", FindingSeverity.High, 70, "SPF was previously present and is now missing.", "Restore the expected SPF policy or validate the approved change."));
        }

        if (previousCheck.DmarcPresent && !dmarcPresent)
        {
            findings.Add(CreateFinding("DMARC weakened", FindingSeverity.High, 75, "DMARC was previously present and is now missing.", "Restore the expected DMARC policy or validate the approved change."));
        }
    }

    private static Finding CreateFinding(string title, string severity, int riskScore, string evidence, string recommendation) =>
        new()
        {
            Title = title,
            Severity = severity,
            RiskScore = riskScore,
            Summary = title,
            Evidence = evidence,
            Recommendation = recommendation
        };

    private static bool SetEquals(IReadOnlyList<string> left, IReadOnlyList<string> right) =>
        new HashSet<string>(left, StringComparer.OrdinalIgnoreCase).SetEquals(right);

    private static bool IsFindingWorthy(Indicator indicator) =>
        string.Equals(indicator.Verdict, IndicatorVerdicts.Malicious, StringComparison.OrdinalIgnoreCase)
        || string.Equals(indicator.Verdict, IndicatorVerdicts.Suspicious, StringComparison.OrdinalIgnoreCase);
}
