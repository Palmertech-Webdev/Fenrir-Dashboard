using System.Net;
using System.Net.Sockets;
using Fenrir.Domain.Enums;

namespace Fenrir.Application.Utilities;

public static class SecurityHelpers
{
    public static string SeverityFromScore(int score) => score switch
    {
        >= 90 => FindingSeverity.Critical,
        >= 70 => FindingSeverity.High,
        >= 40 => FindingSeverity.Medium,
        >= 15 => FindingSeverity.Low,
        _ => FindingSeverity.Informational
    };

    public static int SeverityWeight(string severity) => severity switch
    {
        FindingSeverity.Critical => 100,
        FindingSeverity.High => 80,
        FindingSeverity.Medium => 55,
        FindingSeverity.Low => 25,
        _ => 5
    };

    public static bool IsPrivateIp(string value)
    {
        return IPAddress.TryParse(value, out var ipAddress) && IsPrivateIp(ipAddress);
    }

    public static bool IsPrivateIp(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        var bytes = ipAddress.GetAddressBytes();
        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254);
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal || bytes[0] == 0xfc || bytes[0] == 0xfd;
        }

        return false;
    }
}
