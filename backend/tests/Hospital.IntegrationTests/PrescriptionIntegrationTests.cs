using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

public sealed class PrescriptionIntegrationTests : IClassFixture<HospitalApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HospitalApiFactory _factory;

    public PrescriptionIntegrationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PatientA_CannotReadPatientB_AndIssuedTextChangesOnlyByRevision()
    {
        var patientA = await RegisterPatientAsync("Patient A");
        var patientB = await RegisterPatientAsync("Patient B");
        var doctor = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Doctor);
        var frontDesk = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.FrontDeskStaff);
        var therapist = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Therapist);
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);

        Guid patientAId;
        Guid appointmentId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var chart = await db.Patients.SingleAsync(patient => patient.Email == patientA.Email);
            var treatment = await db.Treatments.FirstAsync();
            patientAId = chart.Id;
            var appointment = new Appointment
            {
                PatientId = chart.Id,
                TreatmentId = treatment.Id,
                RequestedDate = new DateOnly(2026, 10, 6),
                RequestedTimeSlot = "09:00-10:00",
                Status = AppointmentStatus.Approved
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        var draft = await doctor.PostAsJsonAsync("/api/prescriptions", Body(patientAId, appointmentId, "Ashwagandha churna"));
        Assert.Equal(HttpStatusCode.Created, draft.StatusCode);
        var created = await ReadAsync(draft);

        Assert.Equal(HttpStatusCode.NotFound, (await patientA.Client.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.Client.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);

        var issued = await doctor.PostAsync($"/api/prescriptions/{created.Id}/issue", null);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        Assert.Equal("Issued", (await ReadAsync(issued)).Status);

        Assert.Equal(HttpStatusCode.OK, (await patientA.Client.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.Client.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await frontDesk.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await frontDesk.GetAsync("/api/prescriptions")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await therapist.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/prescriptions/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientA.Client.PostAsJsonAsync("/api/prescriptions", Body(patientAId, appointmentId, "Triphala churna"))).StatusCode);

        var edit = await doctor.PutAsJsonAsync(
            $"/api/prescriptions/{created.Id}",
            new { items = new[] { Item("Changed after issue") } });
        Assert.Equal(HttpStatusCode.Conflict, edit.StatusCode);

        var stillOriginal = await ReadAsync(await patientA.Client.GetAsync($"/api/prescriptions/{created.Id}"));
        Assert.Equal("Ashwagandha churna", Assert.Single(stillOriginal.Items).Name);

        var revise = await doctor.PostAsJsonAsync($"/api/prescriptions/{created.Id}/revise", new
        {
            reason = "Dose reduced after nadi pariksha.",
            items = new[] { Item("Brahmi ghrita") }
        });
        Assert.Equal(HttpStatusCode.Created, revise.StatusCode);
        var revised = await ReadAsync(revise);
        Assert.Equal("Issued", revised.Status);
        Assert.Equal(2, revised.RevisionNumber);
        Assert.Equal("Brahmi ghrita", Assert.Single(revised.Items).Name);

        var previous = await ReadAsync(await patientA.Client.GetAsync($"/api/prescriptions/{created.Id}"));
        Assert.Equal("Superseded", previous.Status);
        Assert.Equal("Ashwagandha churna", Assert.Single(previous.Items).Name);

        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.Client.GetAsync($"/api/prescriptions/{revised.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.Client.GetAsync($"/api/prescriptions/{created.Id}/history")).StatusCode);

        var historyResponse = await patientA.Client.GetAsync($"/api/prescriptions/{revised.Id}/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        var history = await historyResponse.Content.ReadFromJsonAsync<HistoryPayload>(JsonOptions);
        Assert.NotNull(history);
        Assert.Equal(2, history.Versions.Count);
        Assert.Equal("Ashwagandha churna", history.Versions[0].Items[0].Name);
        Assert.Equal("Brahmi ghrita", history.Versions[1].Items[0].Name);
        Assert.Equal("Dose reduced after nadi pariksha.", Assert.Single(history.Revisions).Reason);

        var mine = await patientA.Client.GetFromJsonAsync<ListPayload>("/api/prescriptions/mine", JsonOptions);
        Assert.NotNull(mine);
        Assert.Contains(mine.Items, item => item.Id == revised.Id);
        Assert.DoesNotContain(mine.Items, item => item.Id == created.Id);

        var otherMine = await patientB.Client.GetFromJsonAsync<ListPayload>("/api/prescriptions/mine", JsonOptions);
        Assert.NotNull(otherMine);
        Assert.Empty(otherMine.Items);
    }

    private async Task<RegisteredPatient> RegisterPatientAsync(string fullName)
    {
        var client = _factory.CreateClient();
        var email = $"{fullName.Replace(' ', '.').ToLowerInvariant()}.{Guid.NewGuid():N}@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName,
            email,
            phoneNumber = Phone(),
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1991, 4, 12),
            gender = Gender.Female
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<LoginPayload>(JsonOptions);
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return new RegisteredPatient(client, email);
    }

    private static async Task<PrescriptionPayload> ReadAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<PrescriptionPayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload;
    }

    private static object Body(Guid patientId, Guid appointmentId, string name) => new
    {
        patientId,
        appointmentId,
        items = new[] { Item(name) }
    };

    private static object Item(string name) => new
    {
        name,
        dosage = "3 g",
        frequency = "Twice daily after meals",
        duration = "14 days",
        instructions = "Take with warm water"
    };

    private static string Phone()
    {
        var digits = Guid.NewGuid().ToString("N");
        return "07" + digits[..8];
    }

    private sealed record RegisteredPatient(HttpClient Client, string Email);

    private sealed record LoginPayload(string Token);

    private sealed record PrescriptionPayload(
        Guid Id,
        Guid PatientId,
        string Status,
        int RevisionNumber,
        Guid? RevisesPrescriptionId,
        List<ItemPayload> Items);

    private sealed record ItemPayload(Guid Id, string Name, string Dosage, string Frequency, string Duration, string Instructions);

    private sealed record HistoryPayload(
        Guid RootPrescriptionId,
        List<PrescriptionPayload> Versions,
        List<RevisionPayload> Revisions);

    private sealed record RevisionPayload(
        Guid PreviousPrescriptionId,
        Guid RevisedPrescriptionId,
        int RevisionNumber,
        string Reason);

    private sealed record ListPayload(List<PrescriptionPayload> Items, int TotalCount);
}
