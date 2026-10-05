namespace Hospital.Application.Auth;

/// <summary>
/// Patient-facing auth copy. These strings must not include the submitted email,
/// phone, or any other identifier that would confirm an account exists.
/// </summary>
public static class AuthMessages
{
    public const string InvalidCredentials = "Invalid email or password.";

    public const string AccountLocked =
        "Account is temporarily locked due to repeated failed login attempts. Please try again later.";

    public const string RegistrationUnavailable =
        "Unable to create an account with the details provided.";

    public const string TooManyAttempts = "Too many attempts. Please try again later.";
}
