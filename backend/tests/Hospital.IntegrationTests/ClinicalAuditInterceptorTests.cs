using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hospital.IntegrationTests;

public class ClinicalAuditInterceptorTests
{
    [Fact]
    public async Task WritesCreateUpdateAndDelete_WithoutClinicalTextPasswordsOrChat()
    {
        const string diagnosis = "vata-imbalance-secret";
        const string chat = "chat: the password is hunter2 token=session-token-value";
        var userId = Guid.NewGuid();
        await using var db = CreateContext(authenticated: true, userId, out _);

        var patient = new Patient
        {
            Uhid = $"UH{userId:N}"[..12],
            FirstName = "Leela",
            LastName = "Menon",
            Phone = $"95{userId:N}"[..10],
            PasswordHash = "hunter2-password-value",
            Allergies = diagnosis
        };
        var prescription = new Prescription
        {
            PatientId = patient.Id,
            AppointmentId = Guid.NewGuid(),
            DoctorUserId = userId,
            DoctorName = chat
        };
        prescription.RootPrescriptionId = prescription.Id;
        var appointment = new Appointment
        {
            PatientId = patient.Id,
            TreatmentId = Guid.NewGuid(),
            RequestedTimeSlot = "Morning"
        };
        db.AddRange(patient, prescription, appointment);
        await db.SaveChangesAsync();

        prescription.DoctorName = diagnosis;
        appointment.Status = AppointmentStatus.Approved;
        await db.SaveChangesAsync();

        db.Remove(prescription);
        db.Remove(appointment);
        await db.SaveChangesAsync();

        var logs = await db.AuditLogs.AsNoTracking().ToListAsync();
        Assert.Contains(logs, log => log.Action == AuditActions.Create && log.EntityName == AuditEntities.Patient && log.EntityId == patient.Id.ToString());
        Assert.Contains(logs, log => log.Action == AuditActions.Create && log.EntityName == AuditEntities.ClinicalRecord && log.EntityId == prescription.Id.ToString());
        Assert.Contains(logs, log => log.Action == AuditActions.Create && log.EntityName == AuditEntities.Appointment && log.EntityId == appointment.Id.ToString());
        Assert.Contains(logs, log => log.Action == AuditActions.Update && log.EntityName == AuditEntities.ClinicalRecord);
        Assert.Contains(logs, log => log.Action == AuditActions.Update && log.EntityName == AuditEntities.Appointment);
        Assert.Contains(logs, log => log.Action == AuditActions.Delete && log.EntityName == AuditEntities.ClinicalRecord && log.EntityId == prescription.Id.ToString());
        Assert.Contains(logs, log => log.Action == AuditActions.Delete && log.EntityName == AuditEntities.Appointment);

        Assert.All(logs, log =>
        {
            Assert.Equal(userId, log.ActorUserId);
            Assert.Equal(UserRole.Doctor.ToString(), log.ActorRole);
            Assert.Equal("198.51.100.4", log.IpAddress);
            Assert.True(log.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
            Assert.DoesNotContain(diagnosis, log.Details);
            Assert.DoesNotContain("hunter2", log.Details);
            Assert.DoesNotContain("session-token-value", log.Details);
            Assert.DoesNotContain("chat:", log.Details, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(diagnosis, Serialized(log));
            Assert.DoesNotContain("hunter2-password-value", Serialized(log));
            Assert.DoesNotContain("session-token-value", Serialized(log));
        });
    }

    [Fact]
    public async Task UnauthenticatedSave_DoesNotWriteARow()
    {
        await using var db = CreateContext(authenticated: false, Guid.NewGuid(), out _);
        db.Patients.Add(new Patient
        {
            Uhid = "UH-ANON",
            FirstName = "Anon",
            LastName = "Visitor",
            Phone = "9400000001"
        });
        await db.SaveChangesAsync();

        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task SanitizesPasswordTokenAndChat_OnRowsWrittenDirectly()
    {
        await using var db = CreateContext(authenticated: false, Guid.NewGuid(), out _);
        db.AuditLogs.Add(new AuditLog
        {
            Action = "StaffCreated",
            ActorEmail = "admin@hospital.test",
            TargetEmail = "staff@hospital.test",
            Details = "password=hunter2 token=session-token-value chat: full conversation about the herb"
        });
        await db.SaveChangesAsync();

        var log = await db.AuditLogs.SingleAsync();
        Assert.DoesNotContain("hunter2", log.Details);
        Assert.DoesNotContain("session-token-value", log.Details);
        Assert.DoesNotContain("full conversation", log.Details);
    }

    private static string Serialized(AuditLog log) =>
        $"{log.Action}|{log.EntityName}|{log.EntityId}|{log.ActorEmail}|{log.ActorRole}|{log.Details}|{log.IpAddress}";

    private static HospitalDbContext CreateContext(bool authenticated, Guid userId, out StubUser user)
    {
        user = new StubUser
        {
            IsAuthenticated = authenticated,
            UserId = userId,
            Email = "doctor@hospital.test",
            Role = UserRole.Doctor
        };
        var options = new DbContextOptionsBuilder<HospitalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new ClinicalAuditInterceptor(user, new StubAddress()))
            .Options;
        return new HospitalDbContext(options);
    }

    private sealed class StubUser : ICurrentUser
    {
        public bool IsAuthenticated { get; init; }
        public Guid UserId { get; init; }
        public string Email { get; init; } = string.Empty;
        public UserRole Role { get; init; }
    }

    private sealed class StubAddress : IClientAddress
    {
        public string? IpAddress => "198.51.100.4";
    }
}
