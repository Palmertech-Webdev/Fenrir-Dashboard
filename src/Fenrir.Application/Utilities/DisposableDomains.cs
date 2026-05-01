namespace Fenrir.Application.Utilities;

public static class DisposableDomains
{
    private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
    {
        "10minutemail.com",
        "guerrillamail.com",
        "mailinator.com",
        "tempmail.com",
        "temp-mail.org",
        "yopmail.com",
        "throwawaymail.com",
        "trashmail.com"
    };

    public static bool Contains(string domain) => Domains.Contains(domain);
}
