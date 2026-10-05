using Hospital.Api.Middleware;

namespace Hospital.IntegrationTests;

public sealed class ProblemDetailSanitizerTests
{
    [Fact]
    public void EnglishFromPhrase_StaysVisible()
    {
        var message = ProblemDetailSanitizer.PublicMessage(
            "No AI reply draft is waiting for this feedback. Request a draft from the Feedback page first.",
            "The request is invalid.");

        Assert.Contains("from the Feedback", message, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlFromClause_IsHidden()
    {
        var quoted = ProblemDetailSanitizer.PublicMessage(
            "42P01: SELECT id FROM \"users\"",
            "The request is invalid.");
        var qualified = ProblemDetailSanitizer.PublicMessage(
            "missing FROM public.users",
            "The request is invalid.");

        Assert.Equal("The request is invalid.", quoted);
        Assert.Equal("The request is invalid.", qualified);
    }
}
