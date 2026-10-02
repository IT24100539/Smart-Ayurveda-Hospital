using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly HospitalDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(HospitalDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
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
        // Enforce privacy rule: Never log passwords, tokens, or full chat text.
        var sanitizedDetails = SanitizeDetails(details);

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
            Details = sanitizedDetails
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
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
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
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string SanitizeDetails(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var s = raw;
        // Strip out passwords, bearer tokens, or full message bodies if present
        if (s.Contains("Password", StringComparison.OrdinalIgnoreCase))
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)password[""\s:=]+[^;,\s\}]+", "password: [REDACTED]");
        if (s.Contains("Token", StringComparison.OrdinalIgnoreCase))
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)bearer\s+[a-zA-Z0-9\-\._~\+\/]+=*", "Bearer [REDACTED]");
        return s.Length > 500 ? s[..500] + "..." : s;
    }
}
