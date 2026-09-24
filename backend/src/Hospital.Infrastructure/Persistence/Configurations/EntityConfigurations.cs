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
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.DoctorId, x.ScheduledAt });
        builder.HasOne(x => x.Patient).WithMany(x => x.Appointments).HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Doctor).WithMany(x => x.Appointments).HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Consultation).WithOne(x => x.Appointment).HasForeignKey<Consultation>(x => x.AppointmentId);
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

public sealed class TreatmentScheduleConfiguration : IEntityTypeConfiguration<TreatmentSchedule>
{
    public void Configure(EntityTypeBuilder<TreatmentSchedule> builder)
    {
        builder.ToTable("treatment_schedules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayOfWeek).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.MaxSlotsPerDay).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.TreatmentId, x.TherapistId, x.DayOfWeek, x.StartTime })
            .IsUnique()
            .HasDatabaseName("ix_schedules_unique_slot");
        builder.HasIndex(x => new { x.TreatmentId, x.DayOfWeek })
            .HasDatabaseName("ix_schedules_treatment_day");

        builder.HasOne(x => x.Therapist)
            .WithMany(x => x.Schedules)
            .HasForeignKey(x => x.TherapistId)
            .OnDelete(DeleteBehavior.SetNull);
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
