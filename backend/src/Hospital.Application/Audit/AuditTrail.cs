using System.Text.RegularExpressions;

namespace Hospital.Application.Audit;

public static class AuditActions
{
    public const string View = "View";
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
}

public static class AuditEntities
{
    public const string Patient = "Patient";
    public const string Appointment = "Appointment";
    public const string ClinicalRecord = "ClinicalRecord";
    public const string Invoice = "Invoice";
}

public static partial class AuditDetailsSanitizer
{
    public static string Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var sanitized = PasswordPattern().Replace(raw, "$1=[REDACTED]");
        sanitized = TokenPattern().Replace(sanitized, "$1=[REDACTED]");
        sanitized = BearerPattern().Replace(sanitized, "Bearer [REDACTED]");
        sanitized = JwtPattern().Replace(sanitized, "[REDACTED]");
        sanitized = PasswordHashPattern().Replace(sanitized, "[REDACTED]");
        sanitized = ChatPattern().Replace(sanitized, "$1=[REDACTED]");

        return sanitized.Length > 500 ? sanitized[..500] + "..." : sanitized;
    }

    [GeneratedRegex(@"(?i)\b(password|passwd|pwd)\b\s*[""']?\s*[:=]\s*[""']?[^\s""',;}&]+", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"(?i)\b(token|api[_-]?key|secret)\b\s*[""']?\s*[:=]\s*[""']?[^\s""',;}&]+", RegexOptions.CultureInvariant)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"(?i)bearer\s+[A-Za-z0-9\-._~+/]+=*", RegexOptions.CultureInvariant)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b", RegexOptions.CultureInvariant)]
    private static partial Regex JwtPattern();

    [GeneratedRegex(@"\$2[aby]\$\d{2}\$[./A-Za-z0-9]{20,}", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordHashPattern();

    [GeneratedRegex(@"(?i)\b(chat|message|transcript)\b\s*[""']?\s*[:=]\s*.*", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex ChatPattern();
}
