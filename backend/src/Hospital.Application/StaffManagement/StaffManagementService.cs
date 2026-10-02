using System.Security.Cryptography;
using Hospital.Application.Abstractions;
using Hospital.Application.StaffManagement.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.StaffManagement;

public sealed class StaffManagementService : IStaffManagementService
{
    private readonly IUserRepository _users;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public StaffManagementService(
        IUserRepository users,
        IAuditLogRepository auditLogs,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _auditLogs = auditLogs;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<StaffUserDto> CreateStaffAsync(
        CreateStaffUserRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken)
    {
        if (request.Role == UserRole.Patient)
        {
            throw new BadRequestException("Cannot create a staff account with the Patient role.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _users.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"An account with email '{normalizedEmail}' already exists.");
        }

        var tempPassword = !string.IsNullOrWhiteSpace(request.TemporaryPassword)
            ? request.TemporaryPassword.Trim()
            : GenerateSecureTemporaryPassword();

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = request.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.Hash(tempPassword),
            Role = request.Role,
            IsActive = true,
            TokenVersion = 1,
            MustChangePassword = true
        };

        await _users.AddAsync(user, cancellationToken);

        var auditLog = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = "StaffCreated",
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            Details = $"Created staff account with role '{user.Role}'."
        };
        await _auditLogs.AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<StaffUserDto> UpdateRoleAsync(
        Guid targetUserId,
        UpdateStaffRoleRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken)
    {
        if (actorUserId == targetUserId)
        {
            throw new BadRequestException("Staff members cannot change their own role.");
        }

        if (request.Role == UserRole.Patient)
        {
            throw new BadRequestException("Cannot assign the Patient role to a staff member.");
        }

        var user = await _users.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException("Staff user", targetUserId);

        if (user.Role == UserRole.Patient)
        {
            throw new BadRequestException("Target user is not a staff member.");
        }

        if (user.Role == UserRole.Admin && request.Role != UserRole.Admin && user.IsActive)
        {
            var adminCount = await _users.CountActiveAdminsAsync(cancellationToken);
            if (adminCount <= 1)
            {
                throw new BadRequestException("Cannot change the role of the last active Administrator.");
            }
        }

        var oldRole = user.Role;
        user.Role = request.Role;
        user.TokenVersion++;

        var auditLog = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = "StaffRoleUpdated",
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            Details = $"Role changed from '{oldRole}' to '{request.Role}'."
        };
        await _auditLogs.AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<StaffUserDto> UpdateStatusAsync(
        Guid targetUserId,
        UpdateStaffStatusRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken)
    {
        if (actorUserId == targetUserId)
        {
            throw new BadRequestException("Staff members cannot deactivate or change their own account status.");
        }

        var user = await _users.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException("Staff user", targetUserId);

        if (user.Role == UserRole.Patient)
        {
            throw new BadRequestException("Target user is not a staff member.");
        }

        if (user.Role == UserRole.Admin && !request.IsActive && user.IsActive)
        {
            var adminCount = await _users.CountActiveAdminsAsync(cancellationToken);
            if (adminCount <= 1)
            {
                throw new BadRequestException("Cannot deactivate the last active Administrator.");
            }
        }

        user.IsActive = request.IsActive;
        if (!request.IsActive)
        {
            user.TokenVersion++;
        }

        var auditLog = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = request.IsActive ? "StaffReactivated" : "StaffDeactivated",
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            Details = $"Account status set to '{(request.IsActive ? "Active" : "Deactivated")}'."
        };
        await _auditLogs.AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<ForcePasswordResetResponse> ForcePasswordResetAsync(
        Guid targetUserId,
        ForcePasswordResetRequest request,
        Guid actorUserId,
        string actorEmail,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(targetUserId, cancellationToken)
            ?? throw new NotFoundException("Staff user", targetUserId);

        if (user.Role == UserRole.Patient)
        {
            throw new BadRequestException("Target user is not a staff member.");
        }

        var tempPassword = !string.IsNullOrWhiteSpace(request.TemporaryPassword)
            ? request.TemporaryPassword.Trim()
            : GenerateSecureTemporaryPassword();

        user.PasswordHash = _passwordHasher.Hash(tempPassword);
        user.MustChangePassword = true;
        user.TokenVersion++;

        var auditLog = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            Action = "StaffPasswordResetForced",
            TargetUserId = user.Id,
            TargetEmail = user.Email,
            Details = "Administrator forced a temporary password reset."
        };
        await _auditLogs.AddAsync(auditLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ForcePasswordResetResponse(user.Id, user.Email, tempPassword);
    }

    public async Task<(IReadOnlyList<StaffUserDto> Items, int Total)> ListStaffAsync(
        string? query,
        UserRole? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = Math.Clamp(pageSize, 1, 100);
        var skip = (normalizedPage - 1) * normalizedSize;

        var items = await _users.ListStaffAsync(query, role, isActive, skip, normalizedSize, cancellationToken);
        var total = await _users.CountStaffAsync(query, role, isActive, cancellationToken);

        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<(IReadOnlyList<AuditLogDto> Items, int Total)> ListAuditLogsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedSize = Math.Clamp(pageSize, 1, 100);
        var skip = (normalizedPage - 1) * normalizedSize;

        var (items, total) = await _auditLogs.ListAsync(skip, normalizedSize, cancellationToken);

        var dtos = items.Select(x => new AuditLogDto(
            x.Id,
            x.ActorUserId,
            x.ActorEmail,
            x.ActorRole,
            x.Action,
            x.EntityName ?? "User",
            x.EntityId ?? x.TargetUserId.ToString(),
            x.TargetUserId,
            x.TargetEmail,
            x.Details,
            x.CreatedAt)).ToList();

        return (dtos, total);
    }

    private static StaffUserDto ToDto(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.PhoneNumber,
        user.Role,
        user.IsActive,
        user.MustChangePassword,
        user.CreatedAt,
        user.UpdatedAt);

    private static string GenerateSecureTemporaryPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";

        var chars = new char[12];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        var all = upper + lower + digits + special;
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Shuffle
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
