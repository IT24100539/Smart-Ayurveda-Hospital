namespace Hospital.Api.Security;

public sealed class AuthRateLimitOptions
{
    public const string SectionName = "Auth:RateLimit";
    public const string PolicyName = "auth";

    public int PermitLimit { get; set; } = 20;
    public int WindowSeconds { get; set; } = 60;
}
