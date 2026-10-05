using Hospital.Domain.Common;

namespace Hospital.Domain.Entities;

public sealed class DoctorRoster : BaseEntity
{
    public Guid DoctorUserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxPatients { get; set; } = 15;
    public bool IsAvailable { get; set; } = true;
    public string? UnavailabilityReason { get; set; }
    public DateTimeOffset? LeaveStartDate { get; set; }
    public DateTimeOffset? LeaveEndDate { get; set; }
}
