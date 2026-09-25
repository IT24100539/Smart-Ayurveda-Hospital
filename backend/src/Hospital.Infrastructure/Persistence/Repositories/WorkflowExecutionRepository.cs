using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly HospitalDbContext _db;

    public WorkflowExecutionRepository(HospitalDbContext db) => _db = db;

    public Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.WorkflowExecutions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken) =>
        await _db.WorkflowExecutions.AddAsync(execution, cancellationToken);

    public async Task<(IReadOnlyList<WorkflowExecution> Items, int Total)> SearchAsync(
        string? agentName,
        WorkflowApprovalStatus? approvalStatus,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? search,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.WorkflowExecutions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(agentName))
        {
            var agent = agentName.Trim().ToLower();
            query = query.Where(x => x.AgentName.ToLower() == agent);
        }

        if (approvalStatus is not null)
        {
            query = query.Where(x => x.ApprovalStatus == approvalStatus);
        }

        if (from is not null)
        {
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (to is not null)
        {
            query = query.Where(x => x.CreatedAt <= to);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.ObjectiveText.ToLower().Contains(term) ||
                x.AgentName.ToLower().Contains(term) ||
                (x.RelatedEntityType != null && x.RelatedEntityType.ToLower().Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await Order(query, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    private static IOrderedQueryable<WorkflowExecution> Order(IQueryable<WorkflowExecution> query, string? sort)
    {
        var descending = sort?.StartsWith('-') == true;
        var key = (descending ? sort![1..] : sort ?? "-updatedAt").Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();
        return key switch
        {
            "createdat" when descending => query.OrderByDescending(x => x.CreatedAt),
            "createdat" => query.OrderBy(x => x.CreatedAt),
            "agentname" when descending => query.OrderByDescending(x => x.AgentName).ThenByDescending(x => x.UpdatedAt),
            "agentname" => query.OrderBy(x => x.AgentName).ThenByDescending(x => x.UpdatedAt),
            "approvalstatus" when descending => query.OrderByDescending(x => x.ApprovalStatus).ThenByDescending(x => x.UpdatedAt),
            "approvalstatus" => query.OrderBy(x => x.ApprovalStatus).ThenByDescending(x => x.UpdatedAt),
            "finaloutcome" when descending => query.OrderByDescending(x => x.FinalOutcome).ThenByDescending(x => x.UpdatedAt),
            "finaloutcome" => query.OrderBy(x => x.FinalOutcome).ThenByDescending(x => x.UpdatedAt),
            "updatedat" when !descending && sort is not null && !sort.StartsWith('-') => query.OrderBy(x => x.UpdatedAt),
            _ => query.OrderByDescending(x => x.UpdatedAt)
        };
    }
}
