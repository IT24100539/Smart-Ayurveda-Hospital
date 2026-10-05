using System.Text.RegularExpressions;

namespace Hospital.Api.Middleware;

public static partial class ProblemDetailSanitizer
{
    public static string PublicMessage(string? message, string fallback)
    {
        if (string.IsNullOrWhiteSpace(message) || IsUnsafe(message))
        {
            return fallback;
        }

        var trimmed = message.Trim();
        return trimmed.Length <= 300 ? trimmed : fallback;
    }

    public static bool IsUnsafe(string message)
    {
        if (message.Contains('\\') || message.Contains("/src/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (message.Contains("   at ", StringComparison.Ordinal) ||
            message.Contains("stack trace", StringComparison.OrdinalIgnoreCase) ||
            message.Contains(".cs:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return UnsafeMarker().IsMatch(message);
    }

    [GeneratedRegex(
        @"\b(?:Npgsql|PostgresException|SqlException|SqlClient|SELECT\s|INSERT\s|UPDATE\s|DELETE\s+FROM|LINE\s+\d+)\b|\bFROM\s+[""'`]|\bFROM\s+\w+\.",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UnsafeMarker();
}
