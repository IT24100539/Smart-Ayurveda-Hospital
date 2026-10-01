using Hospital.Domain.Enums;

namespace Hospital.Application.StaffManagement.Dtos;

public sealed record CreateStaffUserRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    UserRole Role,
    string? TemporaryPassword = null);

public sealed record UpdateStaffRoleRequest(UserRole Role);

public sealed record UpdateStaffStatusRequest(bool IsActive);

public sealed record ForcePasswordResetRequest(string? TemporaryPassword = null);

public sealed record StaffUserDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    UserRole Role,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ForcePasswordResetResponse(
    Guid UserId,
    string Email,
    string TemporaryPassword);

public sealed record AuditLogDto(
    Guid Id,
    DateTimeOffset Timestamp,
    Guid? ActorUserId,
    string ActorEmail,
    string Action,
    Guid TargetUserId,
    string TargetEmail,
    string Details);
