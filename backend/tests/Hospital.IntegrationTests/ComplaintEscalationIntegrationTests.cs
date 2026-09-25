using FluentAssertions;
using Hospital.Api.Hosting;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class ComplaintEscalationIntegrationTests
{
    private readonly HospitalApiFactory _factory;

    public ComplaintEscalationIntegrationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task EscalateOverdue_OpenComplaintOlderThanFiveDays_BecomesEscalatedAndNotifiesStaff()
    {
        var unassignedSubject = $"Delayed abhyanga {Guid.NewGuid():N}";
        var assignedSubject = $"Shirodhara aftercare {Guid.NewGuid():N}";
        var recentSubject = $"Recent nadi pariksha {Guid.NewGuid():N}";
        Guid unassignedId;
        Guid assignedId;
        Guid recentId;
        Guid adminStaffId;
        Guid doctorStaffId;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var patient = await db.Patients.FirstAsync();
            adminStaffId = (await db.StaffUsers.SingleAsync(x => x.Role == StaffRole.Admin && x.IsActive)).Id;
            doctorStaffId = (await db.StaffUsers.SingleAsync(x => x.Role == StaffRole.Doctor && x.IsActive)).Id;

            var unassigned = NewOpenComplaint(patient.Id, unassignedSubject);
            var assigned = NewOpenComplaint(patient.Id, assignedSubject);
            assigned.AssignedToId = doctorStaffId;
            var recent = NewOpenComplaint(patient.Id, recentSubject);
            db.Complaints.AddRange(unassigned, assigned, recent);
            await db.SaveChangesAsync();

            var overdueAt = DateTimeOffset.UtcNow.AddDays(-6);
            unassigned.CreatedAt = overdueAt;
            assigned.CreatedAt = overdueAt;
            await db.SaveChangesAsync();

            unassignedId = unassigned.Id;
            assignedId = assigned.Id;
            recentId = recent.Id;
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var cutoff = DateTimeOffset.UtcNow.AddDays(-5);
            var storedUnassigned = await db.Complaints.AsNoTracking().SingleAsync(x => x.Id == unassignedId);
            storedUnassigned.Status.Should().Be(ComplaintStatus.Open);
            storedUnassigned.CreatedAt.Should().BeBefore(cutoff);
        }

        var worker = _factory.Services.GetRequiredService<ComplaintEscalationHostedService>();
        await worker.EscalateOverdueAsync(CancellationToken.None);

        await using var verify = _factory.Services.CreateAsyncScope();
        var verifyDb = verify.ServiceProvider.GetRequiredService<HospitalDbContext>();

        var escalatedUnassigned = await verifyDb.Complaints.AsNoTracking().SingleAsync(x => x.Id == unassignedId);
        escalatedUnassigned.Status.Should().Be(ComplaintStatus.Escalated);
        escalatedUnassigned.Priority.Should().Be(ComplaintPriority.High);
        escalatedUnassigned.AssignedToId.Should().Be(adminStaffId);
        escalatedUnassigned.EscalatedAt.Should().NotBeNull();

        var escalatedAssigned = await verifyDb.Complaints.AsNoTracking().SingleAsync(x => x.Id == assignedId);
        escalatedAssigned.Status.Should().Be(ComplaintStatus.Escalated);
        escalatedAssigned.AssignedToId.Should().Be(doctorStaffId);

        var stillOpen = await verifyDb.Complaints.AsNoTracking().SingleAsync(x => x.Id == recentId);
        stillOpen.Status.Should().Be(ComplaintStatus.Open);

        var adminNotice = await verifyDb.Notifications.AsNoTracking()
            .SingleAsync(x => x.StaffUserId == adminStaffId && x.Message.Contains(unassignedSubject));
        adminNotice.Type.Should().Be(NotificationType.ComplaintEscalated);
        adminNotice.Title.Should().Be("Complaint auto-escalated");
        adminNotice.IsRead.Should().BeFalse();

        var doctorNotice = await verifyDb.Notifications.AsNoTracking()
            .SingleAsync(x => x.StaffUserId == doctorStaffId && x.Message.Contains(assignedSubject));
        doctorNotice.Type.Should().Be(NotificationType.ComplaintEscalated);

        var recentNotices = await verifyDb.Notifications.AsNoTracking()
            .Where(x => x.StaffUserId != null && x.Message.Contains(recentSubject))
            .ToListAsync();
        recentNotices.Should().BeEmpty();
    }

    private static Complaint NewOpenComplaint(Guid patientId, string subject) => new()
    {
        PatientId = patientId,
        Subject = subject,
        Description = "The scheduled therapy started long after the booked slot, with no update from the panchakarma wing.",
        Priority = ComplaintPriority.Normal,
        Status = ComplaintStatus.Open
    };
}
