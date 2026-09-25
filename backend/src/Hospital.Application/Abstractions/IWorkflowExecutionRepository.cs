using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Abstractions;

public interface IWorkflowExecutionRepository
{
    Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken);
    Task<(IReadOnlyList<WorkflowExecution> Items, int Total)> SearchAsync(
        string? agentName,
        WorkflowApprovalStatus? approvalStatus,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? search,
        string? sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
