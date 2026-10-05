using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly HospitalDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IClientAddress _clientAddress;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        HospitalDbContext db,
        ICurrentUser currentUser,
        IClientAddress clientAddress,
        ILogger<AuditLogService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _clientAddress = clientAddress;
        _logger = logger;
    }

    public async Task RecordAsync(string action, string entityName, string entityId, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return;
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = _currentUser.UserId,
            ActorEmail = _currentUser.Email,
            ActorRole = _currentUser.Role.ToString(),
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            TargetEmail = string.Empty,
            Details = string.Empty,
            IpAddress = _clientAddress.IpAddress
        });
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[AUDIT] {Action} on {EntityName}:{EntityId} by {ActorRole} {ActorUserId}",
            action,
            entityName,
            entityId,
            _currentUser.Role,
            _currentUser.UserId);
    }

    public async Task LogAsync(
        Guid? actorUserId,
        string actorEmail,
        string? actorRole,
        string action,
        string entityName,
        string entityId,
        string details,
        Guid? targetUserId = null,
        string? targetEmail = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            ActorUserId = actorUserId,
            ActorEmail = actorEmail ?? string.Empty,
            ActorRole = actorRole,
            Action = action ?? string.Empty,
            EntityName = entityName ?? string.Empty,
            EntityId = entityId ?? string.Empty,
            TargetUserId = targetUserId ?? Guid.Empty,
            TargetEmail = targetEmail ?? string.Empty,
            Details = AuditDetailsSanitizer.Sanitize(details),
            IpAddress = _clientAddress.IpAddress
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "[AUDIT] {Action} on {EntityName}:{EntityId} by User {ActorEmail} ({ActorRole})",
            action, entityName, entityId, actorEmail, actorRole);
    }

    public async Task<PagedResult<AuditLogDto>> SearchAsync(
        string? query,
        string? action,
        string? entityName,
        string? entityId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLower();
            q = q.Where(x =>
                x.ActorEmail.ToLower().Contains(term) ||
                x.Details.ToLower().Contains(term) ||
                x.EntityId.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            q = q.Where(x => x.Action == action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            q = q.Where(x => x.EntityName == entityName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            var id = entityId.Trim();
            q = q.Where(x => x.EntityId == id);
        }

        if (fromDate.HasValue)
        {
            q = q.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            q = q.Where(x => x.CreatedAt <= toDate.Value);
        }

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(
                x.Id,
                x.ActorUserId,
                x.ActorEmail,
                x.ActorRole,
                x.Action,
                x.EntityName,
                x.EntityId,
                x.TargetUserId,
                x.TargetEmail,
                x.Details,
                x.CreatedAt,
                x.IpAddress))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

}
