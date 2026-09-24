using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;
using Hospital.Application.Wards;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(PostgreSqlIntegrationTestCollection.Name)]
public sealed class AppointmentAndWardIntegrationTests
{
    private static readonly DateOnly ValidMonday = new(2026, 9, 21);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
    private readonly PostgreSqlHospitalApiFactory _factory;

    public AppointmentAndWardIntegrationTests(PostgreSqlHospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task AppointmentBooking_RoundTrip_ShowsApprovedInMyAppointments()
    {
        await _factory.ResetDatabaseAsync();
        var data = await ArrangeAppointmentDataAsync(maxPatients: 2, existingAppointments: 0, patientCount: 1);

        using var patientClient = _factory.CreateAuthenticatedClient(data.Patients[0].Id, UserRole.Patient);
        var createResponse = await patientClient.PostAsJsonAsync("/api/appointments", Request(data, data.Patients[0].Id));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<AppointmentDto>(JsonOptions);
        created.Should().NotBeNull();

        using var adminClient = _factory.CreateAuthenticatedClient(data.AdminId, UserRole.Admin);
        var approvalResponse = await adminClient.PatchAsJsonAsync($"/api/appointments/{created!.Id}/decision", new
        {
            status = AppointmentStatus.Approved,
            decidedBy = data.AdminId
        });
        approvalResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var mineResponse = await patientClient.GetAsync("/api/appointments/me");
        mineResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var mine = await mineResponse.Content.ReadFromJsonAsync<PagedResult<AppointmentDto>>(JsonOptions);
        mine.Should().NotBeNull();
        mine!.Items.Should().Contain(x => x.Id == created.Id && x.Status == AppointmentStatus.Approved);
    }

    [Fact]
    public async Task ConcurrentBooking_ForLastRemainingSlot_AllowsExactlyOne()
    {
        await _factory.ResetDatabaseAsync();
        var data = await ArrangeAppointmentDataAsync(maxPatients: 2, existingAppointments: 1, patientCount: 3);

        using var firstClient = _factory.CreateAuthenticatedClient(data.Patients[1].Id, UserRole.Patient);
        using var secondClient = _factory.CreateAuthenticatedClient(data.Patients[2].Id, UserRole.Patient);

        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync("/api/appointments", Request(data, data.Patients[1].Id)),
            secondClient.PostAsJsonAsync("/api/appointments", Request(data, data.Patients[2].Id)));

        responses.Count(x => x.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(x => x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest).Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var activeCount = await db.Appointments.CountAsync(x =>
            x.TreatmentId == data.TreatmentId &&
            x.RequestedDate == ValidMonday &&
            x.RequestedTimeSlot == data.TimeSlot &&
            x.Status != AppointmentStatus.Cancelled);
        activeCount.Should().Be(2);
    }

    [Fact]
    public async Task ConcurrentAdmissionApproval_ForOneFreeBed_AssignsExactlyOne()
    {
        await _factory.ResetDatabaseAsync();
        var data = await ArrangeAdmissionDataAsync();
        using var firstClient = _factory.CreateAuthenticatedClient(data.AdminId, UserRole.Admin);
        using var secondClient = _factory.CreateAuthenticatedClient(data.AdminId, UserRole.Admin);

        var body = new { approve = true, decidedBy = data.AdminId };
        var responses = await Task.WhenAll(
            firstClient.PatchAsJsonAsync($"/api/admissions/{data.AdmissionIds[0]}/decision", body),
            secondClient.PatchAsJsonAsync($"/api/admissions/{data.AdmissionIds[1]}/decision", body));

        responses.Count(x => x.StatusCode == HttpStatusCode.NoContent).Should().Be(1);
        responses.Count(x => x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.BadRequest).Should().Be(1);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var admissions = await db.AdmissionRequests
            .Where(x => data.AdmissionIds.Contains(x.Id))
            .ToListAsync();
        admissions.Count(x => x.Status == AdmissionRequestStatus.Approved).Should().Be(1);
        admissions.Count(x => x.BedId == data.BedId).Should().Be(1);
        admissions.Where(x => x.BedId != null).Select(x => x.BedId).Distinct().Should().HaveCount(1);
        (await db.Beds.CountAsync(x => x.WardId == data.WardId && x.IsOccupied)).Should().Be(1);
    }

    private async Task<AppointmentData> ArrangeAppointmentDataAsync(int maxPatients, int existingAppointments, int patientCount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var admin = NewUser(UserRole.Admin, "admin");
        var treatment = new Treatment
        {
            Name = "Integration Abhyanga",
            Description = "Integration test treatment",
            DurationMinutes = 60,
            UnitPrice = 1000
        };
        var schedule = new TreatmentSchedule
        {
            Treatment = treatment,
            DayOfWeek = DayOfWeek.Monday,
            TimeSlot = "10:00-11:00",
            MaxPatients = maxPatients,
            IsActive = true
        };
        var patients = Enumerable.Range(1, patientCount).Select(i => new Patient
        {
            Uhid = $"INT-PATIENT-{i:D2}",
            FirstName = $"Patient{i}",
            LastName = "Integration",
            Phone = $"07100000{i:D2}"
        }).ToList();
        var patientUsers = patients.Select((patient, index) => new User
        {
            Id = patient.Id,
            FullName = $"{patient.FirstName} {patient.LastName}",
            Email = $"patient-{index + 1}@integration.test",
            PhoneNumber = $"07200000{index + 1:D2}",
            PasswordHash = "not-used",
            Role = UserRole.Patient,
            IsActive = true
        });

        db.Users.AddRange(patientUsers.Append(admin));
        db.Patients.AddRange(patients);
        db.TreatmentSchedules.Add(schedule);
        for (var i = 0; i < existingAppointments; i++)
        {
            db.Appointments.Add(new Appointment
            {
                Patient = patients[i],
                Treatment = treatment,
                Schedule = schedule,
                RequestedDate = ValidMonday,
                RequestedTimeSlot = schedule.TimeSlot,
                Status = AppointmentStatus.Pending
            });
        }
        await db.SaveChangesAsync();

        return new AppointmentData(admin.Id, treatment.Id, schedule.Id, schedule.TimeSlot, patients);
    }

    private async Task<AdmissionData> ArrangeAdmissionDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var admin = NewUser(UserRole.Admin, "ward-admin");
        var patients = Enumerable.Range(1, 2).Select(i => new Patient
        {
            Uhid = $"WARD-PATIENT-{i:D2}",
            FirstName = $"WardPatient{i}",
            LastName = "Integration",
            Phone = $"06100000{i:D2}"
        }).ToList();
        var ward = new Ward
        {
            Name = "Integration Ward",
            NameSinhala = "Integration Ward",
            Gender = WardGender.Mixed,
            TotalCapacity = 1
        };
        var bed = new Bed { Ward = ward, BedLabel = "INT-01", IsOccupied = false };
        var admissions = patients.Select(patient => new AdmissionRequest
        {
            Patient = patient,
            Ward = ward,
            Reason = "Integration admission",
            PreferredDate = ValidMonday,
            Status = AdmissionRequestStatus.Pending
        }).ToList();

        db.Add(admin);
        db.AddRange(patients);
        db.Add(bed);
        db.AddRange(admissions);
        await db.SaveChangesAsync();
        return new AdmissionData(admin.Id, ward.Id, bed.Id, admissions.Select(x => x.Id).ToArray());
    }

    private static CreateAppointmentRequest Request(AppointmentData data, Guid patientId) =>
        new(patientId, data.TreatmentId, data.ScheduleId, ValidMonday, data.TimeSlot);

    private static User NewUser(UserRole role, string prefix) => new()
    {
        FullName = $"Integration {role}",
        Email = $"{prefix}@test.local",
        PhoneNumber = $"05000000{(int)role:D2}",
        PasswordHash = "not-used",
        Role = role,
        IsActive = true
    };

    private sealed record AppointmentData(Guid AdminId, Guid TreatmentId, Guid ScheduleId, string TimeSlot, IReadOnlyList<Patient> Patients);
    private sealed record AdmissionData(Guid AdminId, Guid WardId, Guid BedId, Guid[] AdmissionIds);
}