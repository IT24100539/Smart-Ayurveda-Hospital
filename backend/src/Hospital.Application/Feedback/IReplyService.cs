using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;

namespace Hospital.Application.Communication;

public interface IReplyService
{
    Task<ReplyDto> CreateManualReplyAsync(Guid feedbackId, CreateReplyRequest request, CancellationToken cancellationToken);

    /// <summary>A patient posts a reply on visible feedback. It is not an AI draft.</summary>
    Task<ReplyDto> CreatePatientReplyAsync(Guid feedbackId, CreateReplyRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the support agent after the feedback row is already stored.
    /// A timeout or invalid agent response is ignored so submission still succeeds.
    /// Any draft is left unpublished.
    /// </summary>
    Task TryCaptureAnalysisAsync(Feedback feedback, CancellationToken cancellationToken);

    /// <summary>Asks the agent for a draft. The draft is stored and is not posted.</summary>
    Task<ReplyDto> RequestAiDraftAsync(Guid feedbackId, CancellationToken cancellationToken);

    /// <summary>Staff approves, edits, or rejects an AI draft. Only approval or edit publishes it.</summary>
    Task<ReplyDto> DecideDraftAsync(Guid id, ReplyDecisionRequest request, CancellationToken cancellationToken);
}
