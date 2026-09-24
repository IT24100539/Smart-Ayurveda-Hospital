using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Complaint : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public Guid? FeedbackId { get; set; }
    public Feedback? Feedback { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Normal;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;

    public Guid? AssignedTo { get; set; }
    public StaffUser? Assignee { get; set; }
    public DateTimeOffset? EscalatedAt { get; set; }
}
