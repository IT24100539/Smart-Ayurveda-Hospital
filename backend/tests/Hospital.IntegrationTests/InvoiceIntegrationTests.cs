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

public sealed class InvoiceIntegrationTests : IClassFixture<HospitalApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HospitalApiFactory _factory;

    public InvoiceIntegrationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task StaffBillARealVisitAndStay_PatientReadsOnlyTheirOwnIssuedInvoice()
    {
        var patientA = await RegisterPatientAsync("Invoice Patient A");
        var patientB = await RegisterPatientAsync("Invoice Patient B");
        var frontDesk = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.FrontDeskStaff);
        var doctor = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Doctor);
        var therapist = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Therapist);
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);

        Guid patientAId;
        Guid appointmentId;
        Guid admissionId;
        decimal treatmentPrice;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var chart = await db.Patients.SingleAsync(patient => patient.Email == patientA.Email);
            var treatment = await db.Treatments.SingleAsync(item => item.Name == "Abhyanga");
            patientAId = chart.Id;
            treatmentPrice = treatment.UnitPrice;
            var appointment = new Appointment
            {
                PatientId = chart.Id,
                TreatmentId = treatment.Id,
                RequestedDate = new DateOnly(2026, 10, 6),
                RequestedTimeSlot = "09:00-10:00",
                Status = AppointmentStatus.Completed
            };
            var ward = new Ward
            {
                Name = "Invoice Female Ward",
                NameSinhala = "ඉන්වොයිස්",
                Gender = WardGender.Female,
                TotalCapacity = 4
            };
            var bed = new Bed { Ward = ward, BedLabel = "A-01", IsOccupied = true };
            var admission = new AdmissionRequest
            {
                PatientId = chart.Id,
                Ward = ward,
                Bed = bed,
                Reason = "Virechana recovery",
                PreferredDate = new DateOnly(2026, 10, 6),
                Status = AdmissionRequestStatus.Approved,
                DecidedAt = DateTimeOffset.UtcNow
            };
            db.Appointments.Add(appointment);
            db.AdmissionRequests.Add(admission);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
            admissionId = admission.Id;
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await doctor.PostAsJsonAsync("/api/invoices", Body(patientAId, appointmentId, admissionId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientA.Client.PostAsJsonAsync("/api/invoices", Body(patientAId, appointmentId, admissionId))).StatusCode);

        var draft = await frontDesk.PostAsJsonAsync("/api/invoices", Body(patientAId, appointmentId, admissionId));
        Assert.Equal(HttpStatusCode.Created, draft.StatusCode);
        var created = await ReadAsync(draft);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("LKR", created.Currency);
        Assert.Equal(treatmentPrice + 5000m, created.Total);
        Assert.Equal(created.Total, created.Balance);
        Assert.Equal(treatmentPrice, Assert.Single(created.Lines, line => line.Source == "Treatment").LineTotal);
        Assert.Equal(5000m, Assert.Single(created.Lines, line => line.Source == "Admission").LineTotal);

        Assert.Equal(HttpStatusCode.NotFound, (await patientA.Client.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.Client.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await therapist.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await therapist.GetAsync("/api/invoices")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync($"/api/invoices/{created.Id}")).StatusCode);

        var issued = await frontDesk.PostAsync($"/api/invoices/{created.Id}/issue", null);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        Assert.Equal("Issued", (await ReadAsync(issued)).Status);

        Assert.Equal(HttpStatusCode.OK, (await patientA.Client.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.Client.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await doctor.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/invoices/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await frontDesk.PutAsJsonAsync($"/api/invoices/{created.Id}", new { lines = new[] { new { appointmentId, quantity = 1 } } })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await doctor.PostAsJsonAsync($"/api/invoices/{created.Id}/payments", Payment(treatmentPrice, "Cash"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientA.Client.PostAsJsonAsync($"/api/invoices/{created.Id}/payments", Payment(treatmentPrice, "Cash"))).StatusCode);

        var partial = await frontDesk.PostAsJsonAsync($"/api/invoices/{created.Id}/payments", Payment(treatmentPrice, "Cash", "RCPT-1001"));
        Assert.Equal(HttpStatusCode.OK, partial.StatusCode);
        var partlyPaid = await ReadAsync(partial);
        Assert.Equal("Issued", partlyPaid.Status);
        Assert.Equal(treatmentPrice, partlyPaid.AmountPaid);
        Assert.Equal(5000m, partlyPaid.Balance);
        Assert.Equal("RCPT-1001", Assert.Single(partlyPaid.Payments).Reference);

        Assert.Equal(HttpStatusCode.BadRequest, (await frontDesk.PostAsJsonAsync($"/api/invoices/{created.Id}/payments", Payment(5000.01m, "Card"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await frontDesk.PostAsync($"/api/invoices/{created.Id}/cancel", null)).StatusCode);

        var settled = await frontDesk.PostAsJsonAsync($"/api/invoices/{created.Id}/payments", Payment(5000m, "BankTransfer", "TRX-42"));
        Assert.Equal(HttpStatusCode.OK, settled.StatusCode);
        var paid = await ReadAsync(settled);
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(0m, paid.Balance);
        Assert.Equal(2, paid.Payments.Count);

        Assert.Equal(HttpStatusCode.Conflict, (await frontDesk.PostAsJsonAsync($"/api/invoices/{created.Id}/payments", Payment(1m, "Cash"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await frontDesk.PostAsync($"/api/invoices/{created.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await frontDesk.PostAsJsonAsync("/api/invoices", Body(patientAId, appointmentId, admissionId))).StatusCode);

        var mine = await patientA.Client.GetFromJsonAsync<ListPayload>("/api/invoices/mine", JsonOptions);
        Assert.NotNull(mine);
        Assert.Contains(mine.Items, item => item.Id == created.Id && item.Status == "Paid");

        var otherMine = await patientB.Client.GetFromJsonAsync<ListPayload>("/api/invoices/mine", JsonOptions);
        Assert.NotNull(otherMine);
        Assert.DoesNotContain(otherMine.Items, item => item.Id == created.Id);
    }

    [Fact]
    public async Task DraftPaymentIsRejected_AndCancellingADraftReleasesTheVisit()
    {
        var patient = await RegisterPatientAsync("Invoice Patient C");
        var frontDesk = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.FrontDeskStaff);

        Guid patientId;
        Guid appointmentId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var chart = await db.Patients.SingleAsync(item => item.Email == patient.Email);
            var treatment = await db.Treatments.SingleAsync(item => item.Name == "Nasya Treatment");
            patientId = chart.Id;
            var appointment = new Appointment
            {
                PatientId = chart.Id,
                TreatmentId = treatment.Id,
                RequestedDate = new DateOnly(2026, 10, 8),
                RequestedTimeSlot = "14:00-16:00",
                Status = AppointmentStatus.Approved
            };
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
            appointmentId = appointment.Id;
        }

        var pending = await frontDesk.PostAsJsonAsync("/api/invoices", new
        {
            patientId,
            lines = new[] { new { appointmentId = Guid.NewGuid(), quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.NotFound, pending.StatusCode);

        var draftResponse = await frontDesk.PostAsJsonAsync("/api/invoices", new
        {
            patientId,
            lines = new[] { new { appointmentId, quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
        var draft = await ReadAsync(draftResponse);

        var revised = await frontDesk.PutAsJsonAsync($"/api/invoices/{draft.Id}", new
        {
            notes = "Revised draft",
            lines = new[] { new { appointmentId, quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.OK, revised.StatusCode);
        Assert.Equal("Revised draft", (await ReadAsync(revised)).Notes);

        Assert.Equal(HttpStatusCode.Conflict, (await frontDesk.PostAsJsonAsync($"/api/invoices/{draft.Id}/payments", Payment(100m, "Cash"))).StatusCode);

        var cancelled = await frontDesk.PostAsync($"/api/invoices/{draft.Id}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        Assert.Equal("Cancelled", (await ReadAsync(cancelled)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await patient.Client.GetAsync($"/api/invoices/{draft.Id}")).StatusCode);

        var again = await frontDesk.PostAsJsonAsync("/api/invoices", new
        {
            patientId,
            currency = "lkr",
            lines = new[] { new { appointmentId, quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal("LKR", (await ReadAsync(again)).Currency);
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

    private static async Task<InvoicePayload> ReadAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<InvoicePayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload;
    }

    private static object Body(Guid patientId, Guid appointmentId, Guid admissionId) => new
    {
        patientId,
        lines = new object[]
        {
            new { appointmentId, quantity = 1 },
            new { admissionId, quantity = 2, unitPrice = 2500m }
        }
    };

    private static object Payment(decimal amount, string method, string? reference = null) => new
    {
        amount,
        method,
        paidOn = DateTimeOffset.UtcNow,
        reference
    };

    private static string Phone()
    {
        var digits = Guid.NewGuid().ToString("N");
        return "07" + digits[..8];
    }

    private sealed record RegisteredPatient(HttpClient Client, string Email);

    private sealed record LoginPayload(string Token);

    private sealed record InvoicePayload(
        Guid Id,
        Guid PatientId,
        string Currency,
        string Status,
        decimal Total,
        decimal AmountPaid,
        decimal Balance,
        string? Notes,
        List<LinePayload> Lines,
        List<PaymentPayload> Payments);

    private sealed record LinePayload(
        Guid Id,
        string Source,
        string Description,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal);

    private sealed record PaymentPayload(Guid Id, decimal Amount, string Method, string? Reference);

    private sealed record ListPayload(List<InvoicePayload> Items, int TotalCount);
}
