using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

public class Patient : BaseEntity
{
    /// <summary>Unique hospital identifier, e.g. SAH-2026-00001.</summary>
    public string Uhid { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public string? Allergies { get; set; }

    /// <summary>Innate constitution (prakriti).</summary>
    public DoshaType Prakriti { get; set; } = DoshaType.None;

    /// <summary>Current imbalance (vikriti).</summary>
    public DoshaType Vikriti { get; set; } = DoshaType.None;

    public string? PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
