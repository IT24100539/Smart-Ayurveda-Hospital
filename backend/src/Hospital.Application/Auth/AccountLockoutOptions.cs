namespace Hospital.Application.Auth;

public sealed class AccountLockoutOptions
{
    public const string SectionName = "Auth:Lockout";

    /// <summary>Failed password attempts before the account is locked.</summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>How long a lock lasts. A later successful check after this window unlocks the account.</summary>
    public int LockoutMinutes { get; set; } = 15;

    public AccountLockoutOptions Normalized() => new()
    {
        MaxFailedAttempts = Math.Clamp(MaxFailedAttempts, 1, 100),
        LockoutMinutes = Math.Clamp(LockoutMinutes, 1, 24 * 60)
    };
}
