using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed class EmailVerificationService(IDnsLookupService dnsLookup, IFenrirDataStore dataStore) : IEmailVerificationService
{
    public async Task<EmailVerificationResponse> VerifyAsync(EmailVerificationRequest request, CancellationToken cancellationToken)
    {
        var findings = new List<Finding>();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var formatValid = IndicatorClassifier.TryNormalizeEmail(normalizedEmail, out normalizedEmail);
        var domain = formatValid ? normalizedEmail.Split('@', 2)[1] : "";

        IReadOnlyList<string> mxRecords = [];
        IReadOnlyList<string> txtRecords = [];
        IReadOnlyList<string> dmarcRecords = [];
        IReadOnlyList<string> dkimRecords = [];

        if (formatValid)
        {
            mxRecords = await dnsLookup.GetMxRecordsAsync(domain, cancellationToken);
            txtRecords = await dnsLookup.GetTxtRecordsAsync(domain, cancellationToken);
            dmarcRecords = await dnsLookup.GetTxtRecordsAsync($"_dmarc.{domain}", cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.DkimSelector))
            {
                dkimRecords = await dnsLookup.GetTxtRecordsAsync($"{request.DkimSelector.Trim()}._domainkey.{domain}", cancellationToken);
            }
        }

        var mxPresent = mxRecords.Count > 0;
        var spfPresent = txtRecords.Any(IsSpfRecord);
        var dmarcPresent = dmarcRecords.Any(IsDmarcRecord);
        var dkimPresent = string.IsNullOrWhiteSpace(request.DkimSelector) ? (bool?)null : dkimRecords.Any(IsDkimRecord);
        var disposableDomain = !string.IsNullOrEmpty(domain) && DisposableDomains.Contains(domain);

        var trustScore = 100;
        if (!formatValid)
        {
            trustScore -= 80;
            findings.Add(CreateFinding("EmailVerification", "Invalid email format", FindingSeverity.High, 80, request.Email, "Use a syntactically valid email address before trust checks."));
        }

        if (formatValid && !mxPresent)
        {
            trustScore -= 35;
            findings.Add(CreateFinding("EmailVerification", "No MX records found", FindingSeverity.High, 75, domain, "Treat the address as suspicious or undeliverable until ownership can be validated."));
        }

        if (formatValid && !spfPresent)
        {
            trustScore -= 15;
            findings.Add(CreateFinding("EmailVerification", "No SPF record found", FindingSeverity.Medium, 50, domain, "Publish an SPF policy to define authorized senders for this domain."));
        }

        if (formatValid && !dmarcPresent)
        {
            trustScore -= 20;
            findings.Add(CreateFinding("EmailVerification", "No DMARC record found", FindingSeverity.Medium, 55, domain, "Publish a DMARC policy to reduce spoofing risk."));
        }

        if (dkimPresent == false)
        {
            trustScore -= 10;
            findings.Add(CreateFinding("EmailVerification", "DKIM selector not found", FindingSeverity.Low, 25, $"{request.DkimSelector}._domainkey.{domain}", "Verify that the selector is correct or publish the expected DKIM record."));
        }

        if (disposableDomain)
        {
            trustScore -= 30;
            findings.Add(CreateFinding("EmailVerification", "Disposable email domain detected", FindingSeverity.High, 70, domain, "Avoid trusting disposable email domains for account recovery or privileged workflows."));
        }

        trustScore = Math.Clamp(trustScore, 0, 100);
        var trustScoreRisk = trustScore switch
        {
            < 35 => FindingSeverity.High,
            < 70 => FindingSeverity.Medium,
            _ => FindingSeverity.Low
        };
        var findingRisk = findings.Count == 0
            ? FindingSeverity.Low
            : SecurityHelpers.SeverityFromScore(findings.Max(finding => finding.RiskScore));
        var risk = SecurityHelpers.SeverityWeight(findingRisk) > SecurityHelpers.SeverityWeight(trustScoreRisk)
            ? findingRisk
            : trustScoreRisk;

        var summary = BuildSummary(domain, formatValid, mxPresent, spfPresent, dmarcPresent, disposableDomain);

        var check = new EmailCheck
        {
            Email = normalizedEmail,
            Domain = domain,
            FormatValid = formatValid,
            MxPresent = mxPresent,
            SpfPresent = spfPresent,
            DmarcPresent = dmarcPresent,
            DkimSelector = request.DkimSelector,
            DkimPresent = dkimPresent,
            DisposableDomain = disposableDomain,
            Risk = risk,
            TrustScore = trustScore,
            Summary = summary
        };

        await dataStore.AddEmailCheckAsync(check, cancellationToken);
        foreach (var finding in findings)
        {
            finding.RelatedEntityId = check.Id;
            finding.RelatedEntityType = nameof(EmailCheck);
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }

        return new EmailVerificationResponse(
            normalizedEmail,
            domain,
            formatValid,
            mxPresent,
            spfPresent,
            dmarcPresent,
            request.DkimSelector,
            dkimPresent,
            disposableDomain,
            risk,
            trustScore,
            summary,
            findings.Select(f => f.ToDto()).ToArray());
    }

    private static bool IsSpfRecord(string value) => value.Trim().StartsWith("v=spf1", StringComparison.OrdinalIgnoreCase);

    private static bool IsDmarcRecord(string value) => value.Trim().StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase);

    private static bool IsDkimRecord(string value) => value.Contains("v=DKIM1", StringComparison.OrdinalIgnoreCase) || value.Length > 0;

    private static Finding CreateFinding(string module, string title, string severity, int riskScore, string evidence, string recommendation) =>
        new()
        {
            Module = module,
            Type = "EmailVerificationFinding",
            Title = title,
            Severity = severity,
            RiskScore = riskScore,
            Summary = title,
            Evidence = evidence,
            Recommendation = recommendation
        };

    private static string BuildSummary(string domain, bool formatValid, bool mxPresent, bool spfPresent, bool dmarcPresent, bool disposableDomain)
    {
        if (!formatValid)
        {
            return "Email address format is invalid, so trust checks were limited.";
        }

        var strengths = new List<string>();
        if (mxPresent) strengths.Add("MX");
        if (spfPresent) strengths.Add("SPF");
        if (dmarcPresent) strengths.Add("DMARC");

        var gaps = new List<string>();
        if (!mxPresent) gaps.Add("MX");
        if (!spfPresent) gaps.Add("SPF");
        if (!dmarcPresent) gaps.Add("DMARC");
        if (disposableDomain) gaps.Add("disposable-domain");

        return gaps.Count == 0
            ? $"{domain} has the expected email posture records for the MVP checks."
            : $"{domain} has {string.Join(", ", strengths.DefaultIfEmpty("no core email posture records"))}; missing or risky signals: {string.Join(", ", gaps)}.";
    }
}
