namespace Hospital.Domain.Enums;

public enum NotificationType
{
    FeedbackReply = 1,
    ComplaintUpdate = 2,
    ComplaintEscalated = 3,
    General = 4,

    /// <summary>Staff-only alert for high-priority feedback. Hidden from the patient inbox.</summary>
    FeedbackAlert = 5
}
