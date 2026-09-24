using Hospital.Domain.Enums;

namespace Hospital.Application.Communication.Dtos;

public sealed record ReactionRequest(FeedbackReactionType ReactionType);

public sealed record ReactionDto(
    Guid Id,
    Guid FeedbackId,
    FeedbackReactionType ReactionType);
