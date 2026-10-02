using Hospital.Application.Common;
using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string ActorEmail,
    string? ActorRole,
    string Action,
    string EntityName,
    string EntityId,
    Guid TargetUserId,
    string TargetEmail,
    string Details,
    DateTimeOffset CreatedAt);

public interface IAuditLogService
{
    Task LogAsync(
        Guid? actorUserId,
        string actorEmail,
        string? actorRole,
        string action,
        string entityName,
        string entityId,
        string details,
        Guid? targetUserId = null,
        string? targetEmail = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> SearchAsync(
        string? query,
        string? action,
        string? entityName,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
