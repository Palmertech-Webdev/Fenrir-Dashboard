using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Fenrir.Application.Abstractions;
using Fenrir.Application.Mapping;
using Fenrir.Application.Utilities;
using Fenrir.Contracts;
using Fenrir.Domain.Entities;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Services;

public sealed partial class EmailHeaderCheckService(IFenrirDataStore dataStore) : IEmailHeaderCheckService
{
    public async Task<EmailHeaderCheckResponse> CheckAsync(EmailHeaderCheckRequest request, CancellationToken cancellationToken)
    {
        var headers = ParseHeaders(request.RawHeaders);
        var from = GetFirst(headers, "From") ?? "";
        var replyTo = GetFirst(headers, "Reply-To");
        var returnPath = GetFirst(headers, "Return-Path");
        var receivedChain = GetAll(headers, "Received");
        var authenticationResults = string.Join(" ", GetAll(headers, "Authentication-Results"));
        var sendingIps = ExtractIps(string.Join("\n", receivedChain));
        var spfResult = ExtractAuthResult(authenticationResults, "spf");
        var dkimResult = ExtractAuthResult(authenticationResults, "dkim");
        var dmarcResult = ExtractAuthResult(authenticationResults, "dmarc");
        var fromReplyMismatch = HasFromReplyToMismatch(from, replyTo);
        var privateLeak = sendingIps.Any(SecurityHelpers.IsPrivateIp);
        var suspiciousRelay = receivedChain.Count == 0 || sendingIps.Count == 0 || receivedChain.Count > 12;
        var urls = ExtractUrls(request.RawHeaders);
        var domains = ExtractDomains(request.RawHeaders, urls);
        var findings = new List<Finding>();

        AddAuthFinding(findings, "SPF", spfResult, FindingSeverity.Medium, 50);
        AddAuthFinding(findings, "DKIM", dkimResult, FindingSeverity.Medium, 50);
        AddAuthFinding(findings, "DMARC", dmarcResult, FindingSeverity.High, 80);

        if (fromReplyMismatch)
        {
            findings.Add(new Finding
            {
                Module = "EmailHeaders",
                Type = "EmailHeaderFinding",
                Severity = FindingSeverity.Medium,
                RiskScore = 55,
                Title = "From and Reply-To domains differ",
                Summary = "The message asks replies to go to a different domain than the visible sender.",
                Evidence = $"From={from}; Reply-To={replyTo}",
                Recommendation = "Validate the sender identity out of band before replying or acting on the message."
            });
        }

        if (privateLeak)
        {
            findings.Add(new Finding
            {
                Module = "EmailHeaders",
                Type = "EmailHeaderFinding",
                Severity = FindingSeverity.Low,
                RiskScore = 25,
                Title = "Private/internal IP leaked in Received chain",
                Summary = "A private or loopback IP address appears in the routing headers.",
                Evidence = string.Join(", ", sendingIps.Where(SecurityHelpers.IsPrivateIp)),
                Recommendation = "Review mail gateway configuration if this message originated from your environment."
            });
        }

        if (suspiciousRelay)
        {
            findings.Add(new Finding
            {
                Module = "EmailHeaders",
                Type = "EmailHeaderFinding",
                Severity = FindingSeverity.Medium,
                RiskScore = 45,
                Title = "Suspicious relay chain",
                Summary = "The Received chain is missing, unusually long, or lacks extractable sending IPs.",
                Evidence = $"Received headers={receivedChain.Count}; sending IPs={sendingIps.Count}",
                Recommendation = "Treat routing as untrusted and validate the message through mailbox/provider logs."
            });
        }

        var riskScore = findings.Count == 0 ? 10 : findings.Max(f => f.RiskScore);
        var risk = SecurityHelpers.SeverityFromScore(riskScore);
        var summary = findings.Count == 0
            ? "No major authentication, routing, or spoofing risks were detected in the supplied headers."
            : $"{findings.Count} header risk signal(s) detected. Highest severity: {risk}.";

        var check = new EmailHeaderCheck
        {
            From = from,
            ReplyTo = replyTo,
            ReturnPath = returnPath,
            ReceivedChain = receivedChain,
            SendingIps = sendingIps,
            SpfResult = spfResult,
            DkimResult = dkimResult,
            DmarcResult = dmarcResult,
            FromReplyToMismatch = fromReplyMismatch,
            PrivateIpLeakDetected = privateLeak,
            SuspiciousRelayChainDetected = suspiciousRelay,
            HeaderUrls = urls,
            HeaderDomains = domains,
            Risk = risk,
            Summary = summary
        };

        await dataStore.AddEmailHeaderCheckAsync(check, cancellationToken);
        foreach (var finding in findings)
        {
            finding.RelatedEntityId = check.Id;
            finding.RelatedEntityType = nameof(EmailHeaderCheck);
            await dataStore.AddFindingAsync(finding, cancellationToken);
        }

        return new EmailHeaderCheckResponse(
            from,
            replyTo,
            returnPath,
            receivedChain,
            sendingIps,
            spfResult,
            dkimResult,
            dmarcResult,
            fromReplyMismatch,
            suspiciousRelay,
            privateLeak,
            urls,
            domains,
            risk,
            summary,
            findings.Select(f => f.ToDto()).ToArray());
    }

    private static Dictionary<string, List<string>> ParseHeaders(string rawHeaders)
    {
        var headers = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? currentName = null;
        var currentValue = "";

        foreach (var rawLine in rawHeaders.Replace("\r\n", "\n").Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                break;
            }

            if ((line.StartsWith(' ') || line.StartsWith('\t')) && currentName is not null)
            {
                currentValue += " " + line.Trim();
                continue;
            }

            if (currentName is not null)
            {
                AddHeader(headers, currentName, currentValue);
            }

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                currentName = null;
                currentValue = "";
                continue;
            }

            currentName = line[..separatorIndex].Trim();
            currentValue = line[(separatorIndex + 1)..].Trim();
        }

        if (currentName is not null)
        {
            AddHeader(headers, currentName, currentValue);
        }

        return headers;
    }

    private static void AddHeader(Dictionary<string, List<string>> headers, string name, string value)
    {
        if (!headers.TryGetValue(name, out var values))
        {
            values = [];
            headers[name] = values;
        }

        values.Add(value);
    }

    private static string? GetFirst(Dictionary<string, List<string>> headers, string name) =>
        headers.TryGetValue(name, out var values) ? values.FirstOrDefault() : null;

    private static List<string> GetAll(Dictionary<string, List<string>> headers, string name) =>
        headers.TryGetValue(name, out var values) ? values : [];

    private static string? ExtractAuthResult(string authenticationResults, string mechanism)
    {
        var match = Regex.Match(authenticationResults, $@"\b{Regex.Escape(mechanism)}=(?<result>[a-zA-Z]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["result"].Value.ToLowerInvariant() : null;
    }

    private static List<string> ExtractIps(string text) =>
        IpRegex()
            .Matches(text)
            .Select(match => match.Value.Trim('[', ']'))
            .Where(value => IPAddress.TryParse(value, out _))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<string> ExtractUrls(string text) =>
        UrlRegex()
            .Matches(text)
            .Select(match => match.Value.TrimEnd('.', ',', ';', ')', ']'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<string> ExtractDomains(string text, IReadOnlyList<string> urls)
    {
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var url in urls)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                domains.Add(uri.Host.ToLowerInvariant());
            }
        }

        foreach (Match match in DomainRegex().Matches(text))
        {
            domains.Add(match.Value.Trim('.').ToLowerInvariant());
        }

        return domains.ToList();
    }

    private static bool HasFromReplyToMismatch(string from, string? replyTo)
    {
        if (string.IsNullOrWhiteSpace(replyTo))
        {
            return false;
        }

        var fromDomain = TryExtractAddressDomain(from);
        var replyDomain = TryExtractAddressDomain(replyTo);
        return !string.IsNullOrEmpty(fromDomain)
            && !string.IsNullOrEmpty(replyDomain)
            && !string.Equals(fromDomain, replyDomain, StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryExtractAddressDomain(string value)
    {
        try
        {
            var address = new MailAddress(value);
            return address.Host.ToLowerInvariant();
        }
        catch (FormatException)
        {
            var match = EmailRegex().Match(value);
            return match.Success ? match.Groups["domain"].Value.ToLowerInvariant() : null;
        }
    }

    private static void AddAuthFinding(List<Finding> findings, string mechanism, string? result, string severity, int riskScore)
    {
        if (!string.Equals(result, "fail", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(result, "softfail", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        findings.Add(new Finding
        {
            Module = "EmailHeaders",
            Type = "EmailHeaderFinding",
            Severity = severity,
            RiskScore = riskScore,
            Title = $"{mechanism} failed",
            Summary = $"Authentication-Results shows {mechanism.ToLowerInvariant()}={result}.",
            Evidence = $"Authentication-Results shows {mechanism.ToLowerInvariant()}={result}",
            Recommendation = "Treat the message as suspicious and validate sender identity out of band."
        });
    }

    [GeneratedRegex(@"(?:(?:\d{1,3}\.){3}\d{1,3})|(?:\[[0-9a-fA-F:]+\])", RegexOptions.CultureInvariant)]
    private static partial Regex IpRegex();

    [GeneratedRegex(@"https?://[^\s<>'""]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"\b(?<local>[A-Z0-9._%+-]+)@(?<domain>[A-Z0-9.-]+\.[A-Z]{2,})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?\.)+[A-Z]{2,}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();
}
