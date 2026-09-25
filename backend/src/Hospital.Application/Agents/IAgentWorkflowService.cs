using Hospital.Application.Agents.Dtos;

namespace Hospital.Application.Agents;

public interface IAgentWorkflowService
{
    Task<CoordinatorAgentResponse> StartAsync(StartAgentWorkflowRequest request, CancellationToken cancellationToken);
    Task<Workflows.Dtos.WorkflowExecutionDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Workflows.Dtos.WorkflowExecutionDto> ApproveAsync(Guid id, ApproveAgentWorkflowRequest request, CancellationToken cancellationToken);
}
