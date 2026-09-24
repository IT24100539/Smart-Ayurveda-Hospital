using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class FeedbackReply : BaseEntity
{
    public Guid FeedbackId { get; set; }
    public Feedback Feedback { get; set; } = null!;

    /// <summary>Null when the reply is AI-drafted and not yet attributed to a staff approver.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public FeedbackReplyUserRole UserRole { get; set; }
    public string Reply { get; set; } = string.Empty;
    public bool IsAiGenerated { get; set; }
    public FeedbackReplyStatus Status { get; set; } = FeedbackReplyStatus.Draft;
}
