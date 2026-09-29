using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Feedback : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    /// <summary>
    /// Name captured at submit time so the row still displays correctly if shown anonymously
    /// or if the patient later edits their profile name.
    /// </summary>
    public string PatientNameSnapshot { get; set; } = string.Empty;

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? TreatmentId { get; set; }
    public Treatment? Treatment { get; set; }

    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }

    /// <summary>Filled after agent analysis; null until then.</summary>
    public FeedbackSentiment? Sentiment { get; set; }

    /// <summary>Filled after agent analysis; null until then.</summary>
    public FeedbackCategory? Category { get; set; }

    public FeedbackStatus Status { get; set; } = FeedbackStatus.PendingModeration;

    public Guid? ModeratedById { get; set; }
    public StaffUser? Moderator { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }

    public ICollection<FeedbackReaction> Reactions { get; set; } = new List<FeedbackReaction>();
    public ICollection<FeedbackReply> Replies { get; set; } = new List<FeedbackReply>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
