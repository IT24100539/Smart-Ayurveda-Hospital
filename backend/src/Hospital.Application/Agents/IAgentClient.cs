using Hospital.Application.Agents.Dtos;

namespace Hospital.Application.Agents;

/// <summary>
/// Client for the internal-only LangGraph agent service. Never exposed to the public internet.
/// </summary>
public interface IAgentClient
{
    Task<AgentInvokeResponse> InvokeAsync(AgentInvokeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Feedback-support graph. Returns analysis and an optional draft. Never publishes.
    /// </summary>
    Task<FeedbackSupportAgentResponse> DraftFeedbackSupportAsync(
        FeedbackSupportAgentRequest request,
        CancellationToken cancellationToken);

    Task<CoordinatorAgentResponse> CoordinateAsync(
        StartAgentWorkflowRequest request,
        CancellationToken cancellationToken);
}
