using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class HealthAndAuthTests
{
    private readonly HospitalApiFactory _factory;

    public HealthAndAuthTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Patients_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Treatments_WithoutToken_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/treatments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_OpensAPatientRecord()
    {
        var client = _factory.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@example.local";
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Nimal Silva",
            email,
            phoneNumber = $"077{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1992, 3, 4),
            gender = Gender.Male
        }, json);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "response body: {0}", body);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = await db.Patients.SingleAsync(x => x.Email == email);
        patient.FirstName.Should().Be("Nimal");
        patient.LastName.Should().Be("Silva");
        patient.Uhid.Should().StartWith("SAH-");
        patient.DateOfBirth.Should().Be(new DateOnly(1992, 3, 4));
        patient.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public async Task Login_WithSeedAdmin_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@smartayurveda.local",
            password = "ChangeMe!Admin1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload.Should().NotBeNull();
        payload!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PublicFeed_ShowsVisibleComments_AndHidesAnonymousNames()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/feedback");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Anonymous patient");
        json.Should().NotContain("therapist was dismissive");
        json.Should().NotContain("panchakarma package dates");

        var items = JsonSerializer.Deserialize<List<FeedItem>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Public feed payload was empty.");

        var anonymous = items.Single(x => x.Comment.Contains("nadi pariksha slot", StringComparison.Ordinal));
        anonymous.IsAnonymous.Should().BeTrue();
        anonymous.PatientName.Should().Be("Anonymous patient");
        anonymous.PatientId.Should().BeNull();
        anonymous.Status.Should().Be("Visible");

        var named = items.Single(x => x.Comment.Contains("abhyanga session", StringComparison.Ordinal));
        named.IsAnonymous.Should().BeFalse();
        named.PatientName.Should().Be("Meera Nair");
        named.PatientId.Should().NotBeNull();
    }

    [Fact]
    public async Task StaffSearch_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/feedback/staff");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFeedback_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/feedback", new
        {
            rating = 5,
            comment = "The abhyanga was calming.",
            isAnonymous = true
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patients_WithPatientToken_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Patient);
        var response = await client.GetAsync("/api/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Appointments_WithPatientToken_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Patient);
        var response = await client.GetAsync("/api/appointments");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AppointmentStatus_WithPatientToken_ReturnsForbidden()
    {
        using var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Patient);
        var response = await client.PatchAsJsonAsync($"/api/appointments/{Guid.NewGuid()}/status", new
        {
            status = "Approved",
            decidedBy = (Guid?)null
        });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TreatmentPlans_RequiresPatientRole()
    {
        // Unauthenticated -> 401
        var anonClient = _factory.CreateClient();
        var anonRes = await anonClient.GetAsync("/api/patients/me/treatment-plans");
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Staff token -> 403
        using var adminClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Admin);
        var adminRes = await adminClient.GetAsync("/api/patients/me/treatment-plans");
        adminRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Registered patient -> 200 OK
        var client = _factory.CreateClient();
        var email = $"patient.{Guid.NewGuid():N}@test.local";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Anura Kumara",
            email,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1985, 5, 12),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regResponse.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);
        var planRes = await client.GetAsync("/api/patients/me/treatment-plans");
        planRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TreatmentPlans_IgnoresPastDatesForNextDate()
    {
        var client = _factory.CreateClient();
        var email = $"patient.plans.{Guid.NewGuid():N}@test.local";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Plan Patient",
            email,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1985, 5, 12),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regResponse.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = await db.Patients.SingleAsync(x => x.Email == email);
        var treatment = await db.Treatments.FirstAsync();

        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5));
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5));

        // Past appointment
        db.Appointments.Add(new Hospital.Domain.Entities.Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            TreatmentId = treatment.Id,
            RequestedDate = pastDate,
            RequestedTimeSlot = "09:00-10:00",
            Status = Hospital.Domain.Enums.AppointmentStatus.Approved
        });

        // Future appointment
        db.Appointments.Add(new Hospital.Domain.Entities.Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            TreatmentId = treatment.Id,
            RequestedDate = futureDate,
            RequestedTimeSlot = "11:00-12:00",
            Status = Hospital.Domain.Enums.AppointmentStatus.Pending
        });
        await db.SaveChangesAsync();

        var planRes = await client.GetAsync("/api/patients/me/treatment-plans");
        planRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var plans = await planRes.Content.ReadFromJsonAsync<List<Hospital.Api.Controllers.TreatmentPlanDto>>(jsonOptions);
        plans.Should().NotBeNull();
        var plan = plans!.Single(p => p.TreatmentId == treatment.Id);
        plan.SessionCount.Should().Be(2);
        plan.NextDate.Should().Be(futureDate);
    }

    [Fact]
    public async Task RegistrationSummary_RequiresPatientRole_AndReturnsRealProfileData()
    {
        // Unauthenticated -> 401
        var anonClient = _factory.CreateClient();
        var anonRes = await anonClient.GetAsync("/api/patients/me/registration-summary");
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Staff token -> 403
        using var adminClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Admin);
        var adminRes = await adminClient.GetAsync("/api/patients/me/registration-summary");
        adminRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Registered patient -> 200 OK with real profile fields
        var client = _factory.CreateClient();
        var email = $"patient.{Guid.NewGuid():N}@test.local";
        var phone = $"071{Random.Shared.Next(1000000, 9999999)}";
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Kamal Gunaratne",
            email,
            phoneNumber = phone,
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1980, 11, 20),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regResponse.Content.ReadFromJsonAsync<LoginPayload>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);
        var summaryRes = await client.GetAsync("/api/patients/me/registration-summary");
        summaryRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        var summary = await summaryRes.Content.ReadFromJsonAsync<Hospital.Api.Controllers.RegistrationSummaryDto>(jsonOptions);
        summary.Should().NotBeNull();
        summary!.Uhid.Should().StartWith("SAH-");
        summary.FullName.Should().Be("Kamal Gunaratne");
        summary.Phone.Should().Be(phone);
        summary.Email.Should().Be(email);
        summary.DateOfBirth.Should().Be(new DateOnly(1980, 11, 20));
        summary.Gender.Should().Be(Hospital.Domain.Enums.Gender.Male);
    }

    [Fact]
    public async Task CancelAppointment_ValidatesAuthAndOwnership()
    {
        var fakeId = Guid.NewGuid();

        // 1. 401 unauthenticated
        var anonClient = _factory.CreateClient();
        var anonRes = await anonClient.PatchAsync($"/api/appointments/{fakeId}/cancel", null);
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. 403 for staff (Admin)
        using var adminClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Admin);
        var staffRes = await adminClient.PatchAsync($"/api/appointments/{fakeId}/cancel", null);
        staffRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Register owning patient
        var clientOwner = _factory.CreateClient();
        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.local";
        var regOwner = await clientOwner.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Owner Patient",
            email = ownerEmail,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1990, 1, 1),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regOwner.StatusCode.Should().Be(HttpStatusCode.OK);
        var authOwner = await regOwner.Content.ReadFromJsonAsync<LoginPayload>();
        clientOwner.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authOwner!.Token);

        // Register different patient
        var clientOther = _factory.CreateClient();
        var otherEmail = $"other.{Guid.NewGuid():N}@test.local";
        var regOther = await clientOther.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Other Patient",
            email = otherEmail,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1992, 2, 2),
            gender = Hospital.Domain.Enums.Gender.Female
        });
        regOther.StatusCode.Should().Be(HttpStatusCode.OK);
        var authOther = await regOther.Content.ReadFromJsonAsync<LoginPayload>();
        clientOther.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authOther!.Token);

        // Seed an appointment for the owning patient
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = await db.Patients.SingleAsync(x => x.Email == ownerEmail);
        var treatment = await db.Treatments.FirstAsync();

        var appt = new Hospital.Domain.Entities.Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            TreatmentId = treatment.Id,
            RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            RequestedTimeSlot = "10:00-11:00",
            Status = Hospital.Domain.Enums.AppointmentStatus.Pending
        };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        // 3. 403 for different patient
        var diffRes = await clientOther.PatchAsync($"/api/appointments/{appt.Id}/cancel", null);
        diffRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. 204 for owning patient
        var ownerRes = await clientOwner.PatchAsync($"/api/appointments/{appt.Id}/cancel", null);
        ownerRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status in DB is Cancelled
        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var reloaded = await verifyDb.Appointments.FindAsync(appt.Id);
        reloaded!.Status.Should().Be(Hospital.Domain.Enums.AppointmentStatus.Cancelled);
    }

    [Fact]
    public async Task InternalPatientsEndpoint_RejectsMissingOrInvalidKey_AndSucceedsWithValidKey()
    {
        var patientId = Guid.NewGuid();
        var client = _factory.CreateClient();

        // 1. Missing key -> 401 Unauthorized
        var noKeyRes = await client.GetAsync($"/api/internal/patients/{patientId}");
        noKeyRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 2. Invalid key -> 401 Unauthorized
        using var invalidReq = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/patients/{patientId}");
        invalidReq.Headers.Add("X-Internal-Service-Key", "wrong-key");
        var invalidRes = await client.SendAsync(invalidReq);
        invalidRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 3. Valid key with non-existent patient -> 404 NotFound
        using var validNotFoundReq = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/patients/{patientId}");
        validNotFoundReq.Headers.Add("X-Internal-Service-Key", HospitalApiFactory.InternalServiceKey);
        var notFoundRes = await client.SendAsync(validNotFoundReq);
        notFoundRes.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 4. Valid key with existing patient -> 200 OK
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = new Hospital.Domain.Entities.Patient
        {
            Id = Guid.NewGuid(),
            Uhid = "SAH-99999",
            FirstName = "Saman",
            LastName = "Perera",
            DateOfBirth = new DateOnly(1985, 3, 15),
            Gender = Hospital.Domain.Enums.Gender.Male,
            Phone = "0771234567",
            Email = $"saman.{Guid.NewGuid():N}@test.local"
        };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        using var validReq = new HttpRequestMessage(HttpMethod.Get, $"/api/internal/patients/{patient.Id}");
        validReq.Headers.Add("X-Internal-Service-Key", HospitalApiFactory.InternalServiceKey);
        var successRes = await client.SendAsync(validReq);
        successRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await successRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        data.GetProperty("id").GetGuid().Should().Be(patient.Id);
        data.GetProperty("uhid").GetString().Should().Be("SAH-99999");
        data.GetProperty("fullName").GetString().Should().Be("Saman Perera");
    }

    [Fact]
    public async Task PatientsEndpoints_StaffOnly_RejectsPatientWithForbidden()
    {
        var patientClient = _factory.CreateClient();
        var email = $"patient.crud.{Guid.NewGuid():N}@test.local";
        var regRes = await patientClient.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Blocked Patient",
            email,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1995, 5, 5),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regRes.Content.ReadFromJsonAsync<LoginPayload>();
        patientClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        var targetId = Guid.NewGuid();

        // 1. GET /api/patients/{id} -> 403
        var getRes = await patientClient.GetAsync($"/api/patients/{targetId}");
        getRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. POST /api/patients -> 403
        var postRes = await patientClient.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Test",
            lastName = "Person",
            dateOfBirth = "1990-01-01",
            gender = "Male",
            phone = "0711111111",
            email = $"test.{Guid.NewGuid():N}@test.local"
        });
        postRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. PUT /api/patients/{id} -> 403
        var putRes = await patientClient.PutAsJsonAsync($"/api/patients/{targetId}", new
        {
            firstName = "Test",
            lastName = "Updated",
            dateOfBirth = "1990-01-01",
            gender = "Male",
            phone = "0711111111",
            email = $"test.{Guid.NewGuid():N}@test.local"
        });
        putRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Staff (Admin) can access
        using var adminClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Admin);
        var adminGetRes = await adminClient.GetAsync($"/api/patients/{targetId}");
        adminGetRes.StatusCode.Should().Be(HttpStatusCode.NotFound); // Allowed through auth, 404 because GUID doesn't exist
    }

    [Fact]
    public async Task GetAppointmentById_EnforcesOwnershipOrStaff()
    {
        var anonClient = _factory.CreateClient();
        var fakeId = Guid.NewGuid();

        // 1. Unauthenticated -> 401
        var anonRes = await anonClient.GetAsync($"/api/appointments/{fakeId}");
        anonRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Register owner
        var clientOwner = _factory.CreateClient();
        var ownerEmail = $"owner.appt.{Guid.NewGuid():N}@test.local";
        var regOwner = await clientOwner.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Appt Owner",
            email = ownerEmail,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1990, 1, 1),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regOwner.StatusCode.Should().Be(HttpStatusCode.OK);
        var authOwner = await regOwner.Content.ReadFromJsonAsync<LoginPayload>();
        clientOwner.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authOwner!.Token);

        // Register other patient
        var clientOther = _factory.CreateClient();
        var otherEmail = $"other.appt.{Guid.NewGuid():N}@test.local";
        var regOther = await clientOther.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Other Appt Patient",
            email = otherEmail,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1992, 2, 2),
            gender = Hospital.Domain.Enums.Gender.Female
        });
        regOther.StatusCode.Should().Be(HttpStatusCode.OK);
        var authOther = await regOther.Content.ReadFromJsonAsync<LoginPayload>();
        clientOther.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authOther!.Token);

        // Seed appointment
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = await db.Patients.SingleAsync(x => x.Email == ownerEmail);
        var treatment = await db.Treatments.FirstAsync();

        var appt = new Hospital.Domain.Entities.Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            TreatmentId = treatment.Id,
            RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            RequestedTimeSlot = "10:00-11:00",
            Status = Hospital.Domain.Enums.AppointmentStatus.Pending
        };
        db.Appointments.Add(appt);
        await db.SaveChangesAsync();

        // 2. Other patient -> 403 Forbidden
        var otherRes = await clientOther.GetAsync($"/api/appointments/{appt.Id}");
        otherRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Owning patient -> 200 OK
        var ownerRes = await clientOwner.GetAsync($"/api/appointments/{appt.Id}");
        ownerRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Staff (Doctor) -> 200 OK
        using var docClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Doctor);
        var docRes = await docClient.GetAsync($"/api/appointments/{appt.Id}");
        docRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AgentWorkflows_StaffOnly_RejectsPatientWithForbidden()
    {
        var patientClient = _factory.CreateClient();
        var email = $"patient.workflow.{Guid.NewGuid():N}@test.local";
        var regRes = await patientClient.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Workflow Patient",
            email,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1993, 3, 3),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await regRes.Content.ReadFromJsonAsync<LoginPayload>();
        patientClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        var fakeId = Guid.NewGuid();

        // 1. Patient POST /api/agent-workflows/start -> 403 Forbidden
        var postRes = await patientClient.PostAsJsonAsync("/api/agent-workflows/start", new
        {
            workflowType = "Triage",
            payload = "{\"complaint\": \"headache\"}"
        });
        postRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 2. Patient GET /api/agent-workflows/{id} -> 403 Forbidden
        var getRes = await patientClient.GetAsync($"/api/agent-workflows/{fakeId}");
        getRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 3. Staff (Admin) can access
        using var adminClient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), Hospital.Domain.Enums.UserRole.Admin);
        var adminGetRes = await adminClient.GetAsync($"/api/agent-workflows/{fakeId}");
        adminGetRes.StatusCode.Should().Be(HttpStatusCode.NotFound); // Allowed through auth, 404 because GUID doesn't exist
    }

    [Fact]
    public async Task GetComplaintById_DifferentPatient_ReturnsForbidden()
    {
        // 1. Patient A registers and files a complaint
        var clientA = _factory.CreateClient();
        var emailA = $"patient.a.{Guid.NewGuid():N}@test.local";
        var regA = await clientA.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Patient A",
            email = emailA,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1991, 1, 1),
            gender = Hospital.Domain.Enums.Gender.Male
        });
        regA.StatusCode.Should().Be(HttpStatusCode.OK);
        var authA = await regA.Content.ReadFromJsonAsync<LoginPayload>();
        clientA.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authA!.Token);

        var createRes = await clientA.PostAsJsonAsync("/api/complaints", new
        {
            subject = "Facility Noise",
            description = "The room air conditioner was noisy.",
            priority = Hospital.Domain.Enums.ComplaintPriority.Normal
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createRes.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var complaintId = created.GetProperty("id").GetGuid();

        // 2. Patient B registers
        var clientB = _factory.CreateClient();
        var emailB = $"patient.b.{Guid.NewGuid():N}@test.local";
        var regB = await clientB.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Patient B",
            email = emailB,
            phoneNumber = $"071{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1992, 2, 2),
            gender = Hospital.Domain.Enums.Gender.Female
        });
        regB.StatusCode.Should().Be(HttpStatusCode.OK);
        var authB = await regB.Content.ReadFromJsonAsync<LoginPayload>();
        clientB.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authB!.Token);

        // 3. Patient B attempts GET /api/complaints/{complaintId} -> 403 Forbidden
        var diffPatientRes = await clientB.GetAsync($"/api/complaints/{complaintId}");
        diffPatientRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Patient A (owner) GET /api/complaints/{complaintId} -> 200 OK
        var ownerRes = await clientA.GetAsync($"/api/complaints/{complaintId}");
        ownerRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWards_Anonymous_ReturnsWardOccupancyWithoutPatientData()
    {
        var anonClient = _factory.CreateClient();
        var res = await anonClient.GetAsync("/api/wards");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var wards = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        wards.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);

        // Verify that no ward or bed item contains patient identity fields
        foreach (var ward in wards.EnumerateArray())
        {
            ward.TryGetProperty("patientId", out _).Should().BeFalse();
            ward.TryGetProperty("patientName", out _).Should().BeFalse();
            ward.TryGetProperty("uhid", out _).Should().BeFalse();
            ward.TryGetProperty("diagnosis", out _).Should().BeFalse();

            if (ward.TryGetProperty("beds", out var beds) && beds.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var bed in beds.EnumerateArray())
                {
                    bed.TryGetProperty("patientId", out _).Should().BeFalse();
                    bed.TryGetProperty("patientName", out _).Should().BeFalse();
                    bed.TryGetProperty("patient", out _).Should().BeFalse();
                }
            }
        }
    }

    private sealed record LoginPayload(string Token);
    private sealed record FeedItem(
        Guid? PatientId,
        string PatientName,
        string Comment,
        bool IsAnonymous,
        string Status);
}
