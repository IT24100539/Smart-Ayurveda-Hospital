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

    /// <summary>
    /// Treatment-info graph. Answers from the catalogue only, or refuses medical advice.
    /// </summary>
    Task<TreatmentInfoAgentResponse> AskTreatmentInfoAsync(
        TreatmentInfoAgentRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Patient-info graph. Answers about patient administrative record only, or refuses medical advice.
    /// </summary>
    Task<PatientInfoAgentResponse> AskPatientInfoAsync(
        PatientInfoAgentRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Charaka conversation. Ayurveda questions, including ones outside the hospital catalogue.
    /// </summary>
    Task<CharakaAgentResponse> AskCharakaAsync(
        CharakaAgentRequest request,
        CancellationToken cancellationToken);
}
