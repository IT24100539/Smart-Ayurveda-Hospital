using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

public sealed class AuditApiFactory : HospitalApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, AuditRemoteIpStartupFilter>());
    }

    private sealed class AuditRemoteIpStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, next) =>
            {
                context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.9");
                await next();
            });
            next(app);
        };
    }
}

public sealed class AuditLogApiTests : IClassFixture<AuditApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly AuditApiFactory _factory;

    public AuditLogApiTests(AuditApiFactory factory) => _factory = factory;

    [Fact]
    public async Task PatientAndNonAdminStaff_CannotListAuditLogs()
    {
        var patient = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Patient);
        var doctor = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Doctor);
        var anonymous = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Forbidden, (await patient.GetAsync("/api/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await doctor.GetAsync("/api/audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/audit-logs")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanFilterAndPagePatientAuditRows()
    {
        var staffId = Guid.NewGuid();
        var staff = _factory.CreateAuthenticatedClient(staffId, UserRole.FrontDeskStaff);
        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var marker = staffId.ToString("N");

        var first = await CreatePatientAsync(staff, $"98{marker[..8]}");
        var second = await CreatePatientAsync(staff, $"97{marker[..8]}");

        var viewed = await staff.GetAsync($"/api/patients/{first}");
        Assert.Equal(HttpStatusCode.OK, viewed.StatusCode);

        var updated = await staff.PutAsJsonAsync($"/api/patients/{first}", new
        {
            firstName = "Updated",
            lastName = "Chart",
            dateOfBirth = new DateOnly(1991, 2, 2),
            gender = Gender.Male,
            phone = $"98{marker[..8]}",
            address = "chat: the password is hunter2",
            allergies = "secret-allergy-text",
            prakriti = DoshaType.Kapha,
            vikriti = DoshaType.Pitta,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var email = StaffEmail(staffId, UserRole.FrontDeskStaff);
        var createdPage = await ListAsync(admin, $"action=Create&entityName=Patient&query={Uri.EscapeDataString(email)}&page=1&pageSize=1");
        Assert.Equal(2, createdPage.TotalCount);
        Assert.Single(createdPage.Items);
        Assert.Equal(1, createdPage.Page);
        Assert.Equal(1, createdPage.PageSize);

        var secondPage = await ListAsync(admin, $"action=Create&entityName=Patient&query={Uri.EscapeDataString(email)}&page=2&pageSize=1");
        Assert.Single(secondPage.Items);
        Assert.NotEqual(createdPage.Items[0].Id, secondPage.Items[0].Id);

        var viewedRow = await ListAsync(admin, $"action=View&entityName=Patient&entityId={first}");
        var view = Assert.Single(viewedRow.Items);
        AssertAccess(view, staffId, UserRole.FrontDeskStaff, AuditActions.View, AuditEntities.Patient, first);

        var updatedRow = await ListAsync(admin, $"action=Update&entityName=Patient&entityId={first}");
        var update = Assert.Single(updatedRow.Items);
        AssertAccess(update, staffId, UserRole.FrontDeskStaff, AuditActions.Update, AuditEntities.Patient, first);

        var body = JsonSerializer.Serialize(new[] { view, update, createdPage.Items[0], secondPage.Items[0] }, Json);
        Assert.DoesNotContain("hunter2", body);
        Assert.DoesNotContain("secret-allergy-text", body);
        Assert.DoesNotContain("chat:", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(second.ToString(), body);
    }

    [Fact]
    public async Task TreatmentPlanView_RecordsOneClinicalRow()
    {
        var patientUserId = Guid.NewGuid();
        var email = StaffEmail(patientUserId, UserRole.Patient);
        Guid chartId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            chartId = Guid.NewGuid();
            db.Patients.Add(new Patient
            {
                Id = chartId,
                Uhid = $"SAH-{chartId:N}"[..16],
                FirstName = "Asha",
                LastName = "Nair",
                Phone = $"96{chartId:N}"[..10],
                Email = email
            });
            db.SaveChanges();
        }

        var patient = _factory.CreateAuthenticatedClient(patientUserId, UserRole.Patient);
        var plans = await patient.GetAsync("/api/patients/me/treatment-plans");
        Assert.Equal(HttpStatusCode.OK, plans.StatusCode);

        var admin = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Admin);
        var rows = await ListAsync(admin, $"action=View&entityName=ClinicalRecord&entityId={chartId}");
        var row = Assert.Single(rows.Items);
        AssertAccess(row, patientUserId, UserRole.Patient, AuditActions.View, AuditEntities.ClinicalRecord, chartId);
    }

    private async Task<Guid> CreatePatientAsync(HttpClient client, string phone)
    {
        var response = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Meera",
            lastName = "Iyer",
            dateOfBirth = new DateOnly(1990, 6, 15),
            gender = Gender.Female,
            phone,
            address = "chat: do not store this",
            allergies = "secret-allergy-text",
            prakriti = DoshaType.Pitta,
            vikriti = DoshaType.Vata
        });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, body);
        using var json = JsonDocument.Parse(body);
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<PagedResult<AuditLogDto>> ListAsync(HttpClient admin, string query)
    {
        var response = await admin.GetAsync($"/api/audit-logs?{query}");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, body);
        var page = JsonSerializer.Deserialize<PagedResult<AuditLogDto>>(body, Json);
        Assert.NotNull(page);
        return page;
    }

    private static void AssertAccess(AuditLogDto row, Guid userId, UserRole role, string action, string entityName, Guid entityId)
    {
        Assert.Equal(userId, row.ActorUserId);
        Assert.Equal(role.ToString(), row.ActorRole);
        Assert.Equal(action, row.Action);
        Assert.Equal(entityName, row.EntityName);
        Assert.Equal(entityId.ToString(), row.EntityId);
        Assert.Equal("203.0.113.9", row.IpAddress);
        Assert.True(row.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-5));
        Assert.Equal(string.Empty, row.Details);
    }

    private static string StaffEmail(Guid userId, UserRole role) =>
        $"{role.ToString().ToLowerInvariant()}-{userId:N}@integration.test";
}
