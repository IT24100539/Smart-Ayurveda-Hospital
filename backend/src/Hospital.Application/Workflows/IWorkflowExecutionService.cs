using Hospital.Application.Common;
using Hospital.Application.Workflows.Dtos;

namespace Hospital.Application.Workflows;

public interface IWorkflowExecutionService
{
    Task<WorkflowExecutionDto> UpsertAsync(UpsertWorkflowExecutionRequest request, CancellationToken cancellationToken);
    Task<WorkflowExecutionDto> UpdateAsync(Guid id, UpdateWorkflowExecutionRequest request, CancellationToken cancellationToken);
    Task<WorkflowExecutionDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<WorkflowExecutionDto>> SearchAsync(WorkflowExecutionSearchQuery query, CancellationToken cancellationToken);
}
