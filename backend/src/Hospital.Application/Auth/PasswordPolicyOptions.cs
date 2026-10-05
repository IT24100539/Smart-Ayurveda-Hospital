namespace Hospital.Application.Auth;

public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";

    public int MinimumLength { get; set; } = 8;
    public int MaximumLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;

    public PasswordPolicyOptions Normalized()
    {
        var minimum = Math.Clamp(MinimumLength, 1, 256);
        var maximum = Math.Clamp(MaximumLength, minimum, 256);
        return new PasswordPolicyOptions
        {
            MinimumLength = minimum,
            MaximumLength = maximum,
            RequireUppercase = RequireUppercase,
            RequireLowercase = RequireLowercase,
            RequireDigit = RequireDigit,
            RequireNonAlphanumeric = RequireNonAlphanumeric
        };
    }
}
