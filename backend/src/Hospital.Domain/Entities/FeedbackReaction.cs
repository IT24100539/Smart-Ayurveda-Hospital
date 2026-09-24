using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class FeedbackReaction : BaseEntity
{
    public Guid FeedbackId { get; set; }
    public Feedback Feedback { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public FeedbackReactionType ReactionType { get; set; }
}
