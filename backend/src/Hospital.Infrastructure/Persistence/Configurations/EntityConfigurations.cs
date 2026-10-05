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
        builder.Property(x => x.TokenVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.MustChangePassword).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.PasswordResetTokenHash).HasMaxLength(128);
        builder.Property(x => x.PasswordResetTokenExpiresAt);
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
        builder.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Doctor)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.DoctorId);
    }
}

public sealed class TreatmentScheduleConfiguration : IEntityTypeConfiguration<TreatmentSchedule>
{
    public void Configure(EntityTypeBuilder<TreatmentSchedule> builder)
    {
        builder.ToTable("treatment_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.TimeSlot).HasMaxLength(32);
        builder.Property(x => x.MaxSlotsPerDay).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TreatmentId, x.TherapistId, x.DayOfWeek, x.StartTime })
            .IsUnique()
            .HasDatabaseName("ix_schedules_unique_slot");
        builder.HasIndex(x => new { x.TreatmentId, x.DayOfWeek })
            .HasDatabaseName("ix_schedules_treatment_day");

        builder.HasOne(x => x.Treatment).WithMany(x => x.Schedules).HasForeignKey(x => x.TreatmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Therapist)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.TherapistId)
            .OnDelete(DeleteBehavior.SetNull);
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
        builder.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedById).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.ToTable("treatments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.NameSinhala).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.DescriptionSinhala).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.HasMany(x => x.Schedules).WithOne(x => x.Treatment).HasForeignKey(x => x.TreatmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TherapistConfiguration : IEntityTypeConfiguration<Therapist>
{
    public void Configure(EntityTypeBuilder<Therapist> builder)
    {
        builder.ToTable("therapists");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Specialization).HasMaxLength(160).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}



public sealed class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.ToTable("workflow_executions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AgentName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ObjectiveText).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.PlanJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CompletedStepsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ToolResultsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ValidationResultsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ErrorsJson).HasColumnType("jsonb");
        builder.Property(x => x.ApprovalStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.FinalOutcome).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.RelatedEntityType).HasMaxLength(64);
        builder.HasIndex(x => x.AgentName).HasDatabaseName("ix_workflow_executions_agent_name");
        builder.HasIndex(x => x.ApprovalStatus).HasDatabaseName("ix_workflow_executions_approval_status");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_workflow_executions_created_at");
        builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId })
            .HasDatabaseName("ix_workflow_executions_related_entity");
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
        builder.HasOne(x => x.Moderator).WithMany().HasForeignKey(x => x.ModeratedById).OnDelete(DeleteBehavior.SetNull);
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
        builder.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.SetNull);
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

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.ActorRole).HasMaxLength(32);
        builder.Property(x => x.Action).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TargetEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(2000);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.TargetUserId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.EntityName, x.CreatedAt });
    }
}

public sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("doctors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(Doctor.NameMaxLength).IsRequired();
        builder.Property(x => x.Specialty).HasMaxLength(Doctor.SpecialtyMaxLength).IsRequired();
        builder.Property(x => x.Qualifications).HasMaxLength(Doctor.QualificationsMaxLength).IsRequired();
        builder.Property(x => x.Bio).HasMaxLength(Doctor.BioMaxLength);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.IsSample).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.PhotoStorageKey).HasMaxLength(64);
        builder.Property(x => x.PhotoContentType).HasMaxLength(32);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Name);
    }
}

public sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("prescriptions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DoctorName).HasMaxLength(Prescription.DoctorNameMaxLength).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.RevisionNumber).IsRequired();
        builder.Property(x => x.RootPrescriptionId).IsRequired();
        builder.HasIndex(x => x.PatientId).HasDatabaseName("ix_prescriptions_patient_id");
        builder.HasIndex(x => x.RootPrescriptionId).HasDatabaseName("ix_prescriptions_root");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_prescriptions_status");
        builder.HasIndex(x => x.AppointmentId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Draft', 'Issued')")
            .HasDatabaseName("ux_prescriptions_appointment_open");
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.DoctorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Revises)
            .WithMany()
            .HasForeignKey(x => x.RevisesPrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SupersededBy)
            .WithMany()
            .HasForeignKey(x => x.SupersededByPrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items)
            .WithOne(x => x.Prescription)
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("prescription_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(PrescriptionItem.NameMaxLength).IsRequired();
        builder.Property(x => x.Dosage).HasMaxLength(PrescriptionItem.DosageMaxLength).IsRequired();
        builder.Property(x => x.Frequency).HasMaxLength(PrescriptionItem.FrequencyMaxLength).IsRequired();
        builder.Property(x => x.Duration).HasMaxLength(PrescriptionItem.DurationMaxLength).IsRequired();
        builder.Property(x => x.Instructions).HasMaxLength(PrescriptionItem.InstructionsMaxLength).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.HasIndex(x => new { x.PrescriptionId, x.SortOrder }).HasDatabaseName("ix_prescription_items_order");
    }
}

public sealed class PrescriptionRevisionConfiguration : IEntityTypeConfiguration<PrescriptionRevision>
{
    public void Configure(EntityTypeBuilder<PrescriptionRevision> builder)
    {
        builder.ToTable("prescription_revisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(PrescriptionRevision.ReasonMaxLength).IsRequired();
        builder.Property(x => x.RevisionNumber).IsRequired();
        builder.Property(x => x.RevisedAt).IsRequired();
        builder.HasIndex(x => x.PreviousPrescriptionId).HasDatabaseName("ix_prescription_revisions_previous");
        builder.HasIndex(x => x.RevisedPrescriptionId)
            .IsUnique()
            .HasDatabaseName("ux_prescription_revisions_revised");
        builder.HasOne(x => x.PreviousPrescription)
            .WithMany()
            .HasForeignKey(x => x.PreviousPrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RevisedPrescription)
            .WithMany()
            .HasForeignKey(x => x.RevisedPrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RevisedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(Invoice.NumberMaxLength).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(Invoice.CurrencyLength).IsRequired().HasDefaultValue(Invoice.DefaultCurrency);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Total).HasPrecision(14, 2);
        builder.Property(x => x.AmountPaid).HasPrecision(14, 2);
        builder.Property(x => x.Notes).HasMaxLength(Invoice.NotesMaxLength);
        builder.HasIndex(x => x.InvoiceNumber).IsUnique().HasDatabaseName("ux_invoices_number");
        builder.HasIndex(x => x.PatientId).HasDatabaseName("ix_invoices_patient_id");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_invoices_status");
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Payments).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(x => x.Balance);
    }
}

public sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines", table =>
        {
            table.HasCheckConstraint(
                "ck_invoice_lines_one_source",
                "(\"AppointmentId\" IS NOT NULL AND \"AdmissionRequestId\" IS NULL) OR (\"AppointmentId\" IS NULL AND \"AdmissionRequestId\" IS NOT NULL)");
            table.HasCheckConstraint("ck_invoice_lines_quantity", "\"Quantity\" >= 1");
            table.HasCheckConstraint("ck_invoice_lines_unit_price", "\"UnitPrice\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(InvoiceLine.DescriptionMaxLength).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitPrice).HasPrecision(14, 2);
        builder.Property(x => x.LineTotal).HasPrecision(14, 2);
        builder.Property(x => x.OpenSourceKey).HasMaxLength(InvoiceLine.OpenSourceKeyMaxLength);
        builder.HasIndex(x => new { x.InvoiceId, x.SortOrder }).HasDatabaseName("ix_invoice_lines_order");
        builder.HasIndex(x => x.OpenSourceKey)
            .IsUnique()
            .HasFilter("\"OpenSourceKey\" IS NOT NULL")
            .HasDatabaseName("ux_invoice_lines_open_source");
        builder.HasOne(x => x.Treatment).WithMany().HasForeignKey(x => x.TreatmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Appointment).WithMany().HasForeignKey(x => x.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AdmissionRequest).WithMany().HasForeignKey(x => x.AdmissionRequestId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoicePaymentConfiguration : IEntityTypeConfiguration<InvoicePayment>
{
    public void Configure(EntityTypeBuilder<InvoicePayment> builder)
    {
        builder.ToTable("invoice_payments", table =>
        {
            table.HasCheckConstraint("ck_invoice_payments_amount", "\"Amount\" > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(14, 2);
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.PaidOn).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(InvoicePayment.ReferenceMaxLength);
        builder.HasIndex(x => x.InvoiceId).HasDatabaseName("ix_invoice_payments_invoice_id");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PatientDeviceTokenConfiguration : IEntityTypeConfiguration<PatientDeviceToken>
{
    public void Configure(EntityTypeBuilder<PatientDeviceToken> builder)
    {
        builder.ToTable("patient_device_tokens", table =>
        {
            table.HasCheckConstraint(
                "ck_patient_device_tokens_platform",
                "\"Platform\" IN ('android', 'ios', 'web')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Token).HasMaxLength(PatientDeviceToken.TokenMaxLength).IsRequired();
        builder.Property(x => x.Platform).HasMaxLength(PatientDeviceToken.PlatformMaxLength).IsRequired();
        builder.HasIndex(x => x.Token).IsUnique().HasDatabaseName("ux_patient_device_tokens_token");
        builder.HasIndex(x => x.PatientId).HasDatabaseName("ix_patient_device_tokens_patient_id");
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Cascade);
    }
}
