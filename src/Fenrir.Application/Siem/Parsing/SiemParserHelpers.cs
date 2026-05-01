using System.Text.Json;

namespace Fenrir.Application.Siem.Parsing;

internal static class SiemParserHelpers
{
    public static string RawJsonOrDefault(SiemRawEventInput input)
    {
        if (input.RawJson.HasValue)
        {
            return input.RawJson.Value.GetRawText();
        }

        if (!string.IsNullOrWhiteSpace(input.RawText))
        {
            return JsonSerializer.Serialize(new { message = input.RawText });
        }

        return "{}";
    }

    public static string? String(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            if (property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
            {
                return property.ToString();
            }
        }

        return null;
    }

    public static int? Int(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number))
            {
                return number;
            }

            if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out number))
            {
                return number;
            }
        }

        return null;
    }

    public static DateTime? Timestamp(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            var value = String(element, name);
            if (DateTime.TryParse(value, out var parsed))
            {
                return parsed.ToUniversalTime();
            }

            if (TryGetProperty(element, name, out var property) && property.ValueKind == JsonValueKind.Number)
            {
                if (property.TryGetInt64(out var unixSeconds))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
                }

                if (property.TryGetDouble(out var unixDouble))
                {
                    return DateTimeOffset.FromUnixTimeMilliseconds((long)(unixDouble * 1000)).UtcDateTime;
                }
            }
        }

        return null;
    }

    public static string SeverityFromValue(string? value, string fallback = "Low")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var normalised = value.Trim().ToLowerInvariant();
        return normalised switch
        {
            "critical" or "crit" or "fatal" or "emergency" or "alert" => "Critical",
            "high" or "error" or "err" => "High",
            "medium" or "med" or "warning" or "warn" => "Medium",
            "low" or "notice" or "info" or "informational" => "Low",
            _ => fallback
        };
    }

    public static bool TryGetProperty(JsonElement element, string path, out JsonElement value)
    {
        value = default;
        var current = element;
        foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(part, out current))
            {
                return false;
            }
        }

        value = current;
        return true;
    }
}
