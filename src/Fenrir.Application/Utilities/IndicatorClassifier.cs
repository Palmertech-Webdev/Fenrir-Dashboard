using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Utilities;

public sealed record ClassifiedIndicator(string Original, string Normalized, string Type);

public static partial class IndicatorClassifier
{
    public static ClassifiedIndicator Classify(string indicator)
    {
        var original = indicator.Trim();
        if (string.IsNullOrWhiteSpace(original))
        {
            return new ClassifiedIndicator(original, "", IndicatorTypes.Unknown);
        }

        if (IPAddress.TryParse(original, out var ipAddress))
        {
            return new ClassifiedIndicator(original, ipAddress.ToString(), IndicatorTypes.IpAddress);
        }

        if (TryNormalizeUrl(original, out var normalizedUrl))
        {
            return new ClassifiedIndicator(original, normalizedUrl, IndicatorTypes.Url);
        }

        if (IsFileHash(original))
        {
            return new ClassifiedIndicator(original, original.ToLowerInvariant(), IndicatorTypes.FileHash);
        }

        if (TryNormalizeEmail(original, out var normalizedEmail))
        {
            return new ClassifiedIndicator(original, normalizedEmail, IndicatorTypes.EmailAddress);
        }

        if (TryNormalizeDomain(original, out var normalizedDomain))
        {
            return new ClassifiedIndicator(original, normalizedDomain, IndicatorTypes.Domain);
        }

        return new ClassifiedIndicator(original, original.ToLowerInvariant(), IndicatorTypes.Unknown);
    }

    public static bool TryNormalizeDomain(string value, out string domain)
    {
        domain = "";
        var candidate = value.Trim().TrimEnd('.').ToLowerInvariant();
        if (candidate.Length is < 3 or > 253 || candidate.Contains(' ') || candidate.Contains('/'))
        {
            return false;
        }

        try
        {
            candidate = new IdnMapping().GetAscii(candidate);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!DomainRegex().IsMatch(candidate))
        {
            return false;
        }

        domain = candidate;
        return true;
    }

    public static bool TryNormalizeEmail(string value, out string email)
    {
        email = "";
        try
        {
            var address = new MailAddress(value.Trim());
            if (string.IsNullOrWhiteSpace(address.Host) || !address.Address.Contains('@'))
            {
                return false;
            }

            email = address.Address.ToLowerInvariant();
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string? ExtractEmailDomain(string email)
    {
        return TryNormalizeEmail(email, out var normalized)
            ? normalized.Split('@', 2)[1]
            : null;
    }

    private static bool TryNormalizeUrl(string value, out string url)
    {
        url = "";
        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Host = uri.Host.ToLowerInvariant(),
            Fragment = ""
        };

        url = builder.Uri.ToString().TrimEnd('/');
        return true;
    }

    private static bool IsFileHash(string value)
    {
        var candidate = value.Trim();
        return (candidate.Length is 32 or 40 or 64) && HexRegex().IsMatch(candidate);
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DomainRegex();

    [GeneratedRegex("^[a-f0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HexRegex();
}
