using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hospital.IntegrationTests;

[Collection(PostgreSqlIntegrationTestCollection.Name)]
public sealed class InternalSchedulingTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task MissingOrInvalidKey_RejectsAllMachineRoutesWithoutWriting(string? key)
    {
        using var factory = new InternalFactory();
        using var client = factory.Client(key);
        var id = Guid.NewGuid();
        (await client.GetAsync($"/api/internal/wards/{id}/availability")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync($"/api/treatments/{id}/availability?date=2026-09-21")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/internal/admissions", new { patientId = id, wardId = id, reason = "rest", preferredDate = "2026-09-21" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/internal/appointments/check-slot", new { })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<HospitalDbContext>().AdmissionRequests.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ValidKey_WardReturnsOnlyAggregateCapacity()
    {
        using var factory = new InternalFactory();
        var data = await Seed(factory);
        using var client = factory.Client();
        var response = await client.GetAsync($"/api/internal/wards/{data.WardId}/availability");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.EnumerateObject().Select(x => x.Name).Should().BeEquivalentTo("wardId", "wardName", "totalCapacity", "occupiedCapacity", "freeCapacity");
        json.GetProperty("wardId").GetGuid().Should().Be(data.WardId);
        json.GetProperty("wardName").GetString().Should().Be("Internal ward");
        json.GetProperty("totalCapacity").GetInt32().Should().Be(2);
        json.GetProperty("occupiedCapacity").GetInt32().Should().Be(1);
        json.GetProperty("freeCapacity").GetInt32().Should().Be(1);
        (await client.GetAsync($"/api/internal/wards/{Guid.NewGuid()}/availability")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Admission_IsPendingAgentRequest_AndDoesNotAllocate(bool snakeCase)
    {
        using var factory = new InternalFactory();
        var data = await Seed(factory);
        using var client = factory.Client();
        var body = new Dictionary<string, object>
        {
            [snakeCase ? "patient_id" : "patientId"] = data.PatientId,
            [snakeCase ? "ward_id" : "wardId"] = data.WardId,
            [snakeCase ? "preferred_date" : "preferredDate"] = "2026-09-21",
            ["reason"] = "Agent requested admission",
            ["status"] = "Approved", ["requestedByAgent"] = false, ["bedId"] = data.FreeBedId
        };
        var response = await client.PostAsJsonAsync("/api/internal/admissions", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var admission = await db.AdmissionRequests.SingleAsync();
        admission.Id.Should().Be(json.GetProperty("admissionRequestId").GetGuid());
        admission.PatientId.Should().Be(data.PatientId);
        admission.WardId.Should().Be(data.WardId);
        admission.Reason.Should().Be("Agent requested admission");
        admission.PreferredDate.Should().Be(new DateOnly(2026, 9, 21));
        admission.Status.Should().Be(AdmissionRequestStatus.Pending);
        admission.RequestedByAgent.Should().BeTrue();
        admission.BedId.Should().BeNull();
        admission.DecidedBy.Should().BeNull();
        admission.DecidedAt.Should().BeNull();
        (await db.Beds.SingleAsync(x => x.Id == data.FreeBedId)).IsOccupied.Should().BeFalse();
        (await db.Beds.CountAsync(x => x.IsOccupied)).Should().Be(1);
    }

    [Fact]
    public async Task AuthenticationSchemes_AreIsolated_AndUnconfiguredKeyFailsClosed()
    {
        using var factory = new InternalFactory();
        var data = await Seed(factory);
        using var client = factory.Client();
        (await client.GetAsync($"/api/wards/{data.WardId}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PatchAsJsonAsync($"/api/admissions/{Guid.NewGuid()}/decision", new { approve = true, decidedBy = Guid.NewGuid() })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var scope = factory.Services.CreateScope();
        var (token, _) = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().Create(new User
        { FullName = "Admin", Email = "admin@test.local", Role = UserRole.Admin, IsActive = true });
        client.DefaultRequestHeaders.Remove("X-Internal-Service-Key");
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        (await client.GetAsync($"/api/wards/{data.WardId}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/internal/wards/{data.WardId}/availability")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using var unconfigured = new InternalFactory("");
        using var unconfiguredClient = unconfigured.Client();
        (await unconfiguredClient.GetAsync($"/api/internal/wards/{data.WardId}/availability")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(true, "2026-09-21", 1, false, true)]
    [InlineData(false, "2026-09-21", 1, false, false)]
    [InlineData(true, "2026-09-22", 1, false, false)]
    [InlineData(true, "2026-09-21", 1, true, false)]
    [InlineData(true, "2026-09-21", 0, true, true)]
    public async Task TreatmentAvailability_UsesScheduleAndCapacityRules(bool active, string date, int maxPatients, bool booked, bool available)
    {
        using var factory = new InternalFactory();
        var data = await Seed(factory);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var treatment = new Treatment { Name = "Test treatment" };
        var schedule = new TreatmentSchedule { Treatment = treatment, IsActive = active, DayOfWeek = DayOfWeek.Monday, TimeSlot = "10:00-11:00", MaxPatients = maxPatients };
        db.TreatmentSchedules.Add(schedule);
        if (booked) db.Appointments.Add(new Appointment { Treatment = treatment, Schedule = schedule, PatientId = data.PatientId,
            RequestedDate = DateOnly.Parse(date), RequestedTimeSlot = schedule.TimeSlot, Status = AppointmentStatus.Pending });
        await db.SaveChangesAsync();
        using var client = factory.Client();
        var response = await client.GetAsync($"/api/treatments/{treatment.Id}/availability?date={date}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("treatmentId").GetGuid().Should().Be(treatment.Id);
        json.GetProperty("date").GetString().Should().Be(date);
        json.GetProperty("available").GetBoolean().Should().Be(available);
        if (!available) json.GetProperty("reason").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TreatmentAvailability_NoScheduleIsUnavailable_AndInvalidInputsAreRejected()
    {
        using var factory = new InternalFactory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var treatment = new Treatment { Name = "No schedule" };
        db.Treatments.Add(treatment);
        await db.SaveChangesAsync();
        using var client = factory.Client();
        var json = await client.GetFromJsonAsync<JsonElement>($"/api/treatments/{treatment.Id}/availability?date=2026-09-21");
        json.GetProperty("available").GetBoolean().Should().BeFalse();
        (await client.GetAsync($"/api/treatments/{Guid.NewGuid()}/availability?date=2026-09-21")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/treatments/{treatment.Id}/availability")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await client.GetAsync($"/api/treatments/{treatment.Id}/availability?date=invalid")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<(Guid PatientId, Guid WardId, Guid FreeBedId)> Seed(InternalFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = new Patient { Uhid = "INTERNAL-01", FirstName = "Private", LastName = "Patient", Phone = "0700000000" };
        var ward = new Ward { Name = "Internal ward", TotalCapacity = 2 };
        var free = new Bed { Ward = ward, BedLabel = "PRIVATE-01" };
        db.AddRange(patient, free, new Bed { Ward = ward, BedLabel = "PRIVATE-02", IsOccupied = true });
        await db.SaveChangesAsync();
        return (patient.Id, ward.Id, free.Id);
    }

    private sealed class InternalFactory(string configuredKey = "test-internal-service-key") : WebApplicationFactory<Program>
    {
        private readonly string _database = Guid.NewGuid().ToString();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["InternalService:Key"] = configuredKey }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<HospitalDbContext>>();
                services.AddDbContext<HospitalDbContext>(options => options.UseInMemoryDatabase(_database));
            });
        }
        public HttpClient Client(string? key = "test-internal-service-key")
        {
            var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            if (key is not null) client.DefaultRequestHeaders.Add("X-Internal-Service-Key", key);
            return client;
        }
    }
}