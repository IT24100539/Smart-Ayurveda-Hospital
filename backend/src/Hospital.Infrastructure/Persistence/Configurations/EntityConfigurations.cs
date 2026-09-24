using Hospital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hospital.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
    }
}

public sealed class StaffUserConfiguration : IEntityTypeConfiguration<StaffUser>
{
    public void Configure(EntityTypeBuilder<StaffUser> builder)
    {
        builder.ToTable("staff_users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Specialization).HasMaxLength(160);
        builder.Property(x => x.Phone).HasMaxLength(20);
    }
}

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Uhid).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.Uhid).IsUnique();
        builder.HasIndex(x => x.Phone).IsUnique();
        builder.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.BloodGroup).HasMaxLength(8);
        builder.Property(x => x.Allergies).HasMaxLength(1000);
        builder.Property(x => x.PasswordHash).HasMaxLength(256);
    }
}

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestedTimeSlot).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TreatmentId, x.RequestedDate })
            .HasDatabaseName("ix_appointments_treatment_requested_date");
        builder.HasIndex(x => new { x.PatientId, x.TreatmentId, x.RequestedDate, x.RequestedTimeSlot })
            .IsUnique()
            .HasFilter("\"Status\" <> 'Cancelled'")
            .HasDatabaseName("ux_appointments_patient_treatment_slot_active");
        builder.HasOne(x => x.Patient).WithMany(x => x.Appointments).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Treatment).WithMany(x => x.Appointments).HasForeignKey(x => x.TreatmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Schedule).WithMany(x => x.Appointments).HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Consultation).WithOne(x => x.Appointment).HasForeignKey<Consultation>(x => x.AppointmentId);
    }
}

public sealed class TreatmentScheduleConfiguration : IEntityTypeConfiguration<TreatmentSchedule>
{
    public void Configure(EntityTypeBuilder<TreatmentSchedule> builder)
    {
        builder.ToTable("treatment_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TimeSlot).HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Treatment).WithMany(x => x.Schedules).HasForeignKey(x => x.TreatmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TreatmentId, x.DayOfWeek, x.TimeSlot });
    }
}

public sealed class WardConfiguration : IEntityTypeConfiguration<Ward>
{
    public void Configure(EntityTypeBuilder<Ward> builder)
    {
        builder.ToTable("wards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.NameSinhala).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Gender).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.HasMany(x => x.Beds).WithOne(x => x.Ward).HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BedConfiguration : IEntityTypeConfiguration<Bed>
{
    public void Configure(EntityTypeBuilder<Bed> builder)
    {
        builder.ToTable("beds");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BedLabel).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => new { x.WardId, x.BedLabel }).IsUnique();
    }
}

public sealed class AdmissionRequestConfiguration : IEntityTypeConfiguration<AdmissionRequest>
{
    public void Configure(EntityTypeBuilder<AdmissionRequest> builder)
    {
        builder.ToTable("admission_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Patient).WithMany(x => x.AdmissionRequests).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Ward).WithMany(x => x.AdmissionRequests).HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bed).WithMany(x => x.AdmissionRequests).HasForeignKey(x => x.BedId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        builder.ToTable("consultations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChiefComplaint).HasMaxLength(1000).IsRequired();
        builder.HasOne(x => x.Prescription).WithOne(x => x.Consultation).HasForeignKey<Prescription>(x => x.ConsultationId);
    }
}

public sealed class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.ToTable("treatments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
    }
}

public sealed class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("medicines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Manufacturer).HasMaxLength(160);
        builder.Property(x => x.DosageGuidelines).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Contraindications).HasMaxLength(1000);
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
    }
}

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasMany(x => x.Items).WithOne(x => x.Prescription).HasForeignKey(x => x.PrescriptionId);
    }
}

public sealed class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("prescription_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Dosage).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Frequency).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Instructions).HasMaxLength(500);
        builder.HasOne(x => x.Medicine).WithMany().HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.Property(x => x.Subtotal).HasPrecision(12, 2);
        builder.Property(x => x.Tax).HasPrecision(12, 2);
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.HasOne(x => x.Patient).WithMany(x => x.Invoices).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId);
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(240).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.LineTotal).HasPrecision(12, 2);
    }
}

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("feedbacks", table =>
        {
            table.HasCheckConstraint("ck_feedbacks_rating", "\"Rating\" >= 1 AND \"Rating\" <= 5");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PatientNameSnapshot).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Sentiment);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Patient).WithMany(x => x.Feedbacks).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Appointment).WithMany(x => x.Feedbacks).HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Treatment).WithMany(x => x.Feedbacks).HasForeignKey(x => x.TreatmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Moderator).WithMany().HasForeignKey(x => x.ModeratedBy).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.Reactions).WithOne(x => x.Feedback).HasForeignKey(x => x.FeedbackId);
        builder.HasMany(x => x.Replies).WithOne(x => x.Feedback).HasForeignKey(x => x.FeedbackId);
    }
}

public sealed class FeedbackReactionConfiguration : IEntityTypeConfiguration<FeedbackReaction>
{
    public void Configure(EntityTypeBuilder<FeedbackReaction> builder)
    {
        builder.ToTable("feedback_reactions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.FeedbackId, x.UserId }).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FeedbackReplyConfiguration : IEntityTypeConfiguration<FeedbackReply>
{
    public void Configure(EntityTypeBuilder<FeedbackReply> builder)
    {
        builder.ToTable("feedback_replies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reply).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.FeedbackId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class ComplaintConfiguration : IEntityTypeConfiguration<Complaint>
{
    public void Configure(EntityTypeBuilder<Complaint> builder)
    {
        builder.ToTable("complaints");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Patient).WithMany(x => x.Complaints).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssignedTo).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Feedback).WithMany(x => x.Complaints).HasForeignKey(x => x.FeedbackId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.PatientId, x.IsRead });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Patient).WithMany(x => x.Notifications).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.StaffRecipient).WithMany().HasForeignKey(x => x.StaffUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
