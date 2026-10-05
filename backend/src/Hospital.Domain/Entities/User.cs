using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

/// <summary>
/// Shared identity for staff portal and patient app.
/// Clinical fields live on <see cref="Patient"/> (Member 1), which should reference this entity by UserId.
/// </summary>
public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Patient;
    public bool IsActive { get; set; } = true;
    public int TokenVersion { get; set; } = 1;
    public bool MustChangePassword { get; set; } = false;
    public int FailedLoginCount { get; set; } = 0;
    public DateTimeOffset? LockoutEnd { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTimeOffset? PasswordResetTokenExpiresAt { get; set; }
}
