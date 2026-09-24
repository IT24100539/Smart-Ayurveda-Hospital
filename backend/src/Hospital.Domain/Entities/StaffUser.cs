using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class StaffUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public StaffRole Role { get; set; }
    public string? Specialization { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
}
