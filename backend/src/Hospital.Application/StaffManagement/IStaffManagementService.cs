using Hospital.Application.Abstractions;
using Hospital.Application.StaffManagement.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.StaffManagement;

public interface IStaffManagementService
{
    Task<StaffUserDto> CreateStaffAsync(
        CreateStaffUserRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken);

    Task<StaffUserDto> UpdateRoleAsync(
        Guid targetUserId,
        UpdateStaffRoleRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken);

    Task<StaffUserDto> UpdateStatusAsync(
        Guid targetUserId,
        UpdateStaffStatusRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken);

    Task<ForcePasswordResetResponse> ForcePasswordResetAsync(
        Guid targetUserId,
        ForcePasswordResetRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<StaffUserDto> Items, int Total)> ListStaffAsync(
        string? query,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<AuditLogDto> Items, int Total)> ListAuditLogsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
