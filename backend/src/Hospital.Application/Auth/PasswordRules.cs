using System.Text.RegularExpressions;

namespace Hospital.Application.Auth;

public static class PasswordRules
{
    /// <summary>Kept in step with the Flutter register screen.</summary>
    public const string SpecialCharacterPattern = @"[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]";

    public static IReadOnlyList<string> Evaluate(string? password, PasswordPolicyOptions? policy)
    {
        var rules = (policy ?? new PasswordPolicyOptions()).Normalized();
        if (string.IsNullOrEmpty(password))
        {
            return new[] { "Password is required." };
        }

        var errors = new List<string>();
        if (password.Length < rules.MinimumLength)
        {
            errors.Add($"Password must be at least {rules.MinimumLength} characters long.");
        }

        if (password.Length > rules.MaximumLength)
        {
            errors.Add($"Password cannot exceed {rules.MaximumLength} characters.");
        }

        if (rules.RequireUppercase && !Regex.IsMatch(password, "[A-Z]"))
        {
            errors.Add("Password must contain at least one uppercase letter.");
        }

        if (rules.RequireLowercase && !Regex.IsMatch(password, "[a-z]"))
        {
            errors.Add("Password must contain at least one lowercase letter.");
        }

        if (rules.RequireDigit && !Regex.IsMatch(password, "[0-9]"))
        {
            errors.Add("Password must contain at least one digit.");
        }

        if (rules.RequireNonAlphanumeric && !Regex.IsMatch(password, SpecialCharacterPattern))
        {
            errors.Add("Password must contain at least one special character.");
        }

        return errors;
    }
}
