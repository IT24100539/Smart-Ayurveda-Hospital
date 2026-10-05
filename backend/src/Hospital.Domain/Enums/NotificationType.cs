namespace Hospital.Domain.Enums;

public enum NotificationType
{
    FeedbackReply = 1,
    ComplaintUpdate = 2,
    ComplaintEscalated = 3,
    General = 4,

    /// <summary>Staff-only alert for high-priority feedback. Hidden from the patient inbox.</summary>
    FeedbackAlert = 5,

    AppointmentApproved = 6,
    AppointmentRejected = 7,
    AppointmentRescheduled = 8,
    AppointmentCancelled = 9,
    PrescriptionIssued = 10,
    InvoiceIssued = 11
}
