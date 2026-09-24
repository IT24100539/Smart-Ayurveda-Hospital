using Hospital.Domain.Enums;

namespace Hospital.Application.Communication.Dtos;

public sealed record CreateReplyRequest(string Reply);

public enum ReplyDecision
{
    Approve = 1,
    Edit = 2,
    Reject = 3,

    /// <summary>Updates the draft text and leaves it unpublished.</summary>
    Save = 4
}

/// <summary>
/// Staff decision on an AI draft. <see cref="Reply"/> is required when the decision is Edit.
/// </summary>
public sealed record ReplyDecisionRequest(ReplyDecision Decision, string? Reply);

public sealed record ReplyDto(
    Guid Id,
    Guid FeedbackId,
    Guid? UserId,
    FeedbackReplyUserRole UserRole,
    string Reply,
    bool IsAiGenerated,
    FeedbackReplyStatus Status,
    DateTimeOffset CreatedAt);
