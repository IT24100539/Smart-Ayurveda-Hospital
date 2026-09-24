using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }

    /// <summary>
    /// Set when the notice is for a staff member. Patient inbox queries ignore those rows.
    /// </summary>
    public Guid? StaffUserId { get; set; }
    public StaffUser? StaffRecipient { get; set; }
}
