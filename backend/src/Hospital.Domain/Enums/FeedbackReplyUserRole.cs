namespace Hospital.Domain.Enums;

/// <summary>
/// Author side of a feedback reply. Distinct from <see cref="UserRole"/> (portal identity).
/// </summary>
public enum FeedbackReplyUserRole
{
    Staff = 1,
    Patient = 2
}
