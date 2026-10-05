using Hospital.Application.Audit;

namespace Hospital.UnitTests;

public class AuditDetailsSanitizerTests
{
    [Fact]
    public void Sanitize_RedactsPasswordsTokensAndChatText()
    {
        const string raw =
            "password=hunter2 token: abc.def bearer eyJhbGciOiJIUzI1NiJ9.payloadvalue.signaturevalue " +
            "$2a$11$abcdefghijklmnopqrstuv chat: patient asked for the herb dose";

        var sanitized = AuditDetailsSanitizer.Sanitize(raw);

        Assert.DoesNotContain("hunter2", sanitized);
        Assert.DoesNotContain("abc.def", sanitized);
        Assert.DoesNotContain("eyJhbGciOiJIUzI1NiJ9", sanitized);
        Assert.DoesNotContain("abcdefghijklmnopqrstuv", sanitized);
        Assert.DoesNotContain("herb dose", sanitized);
        Assert.Contains("password=[REDACTED]", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("chat=[REDACTED]", sanitized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sanitize_LeavesOrdinaryAuditText()
    {
        const string raw = "Role changed from 'FrontDeskStaff' to 'Doctor'.";

        Assert.Equal(raw, AuditDetailsSanitizer.Sanitize(raw));
    }
}
