using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Hospital.Api.Controllers;
using Hospital.Application.Abstractions;
using Hospital.Application.Auth.Dtos;
using Hospital.Application.StaffManagement.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hospital.IntegrationTests;

public sealed class StaffManagementIntegrationTests : IClassFixture<HospitalApiFactory>
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly HospitalApiFactory _factory;

    public StaffManagementIntegrationTests(HospitalApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StaffEndpoints_Unauthenticated_Return401()
    {
        var client = _factory.CreateClient();

        var listRes = await client.GetAsync("/api/admin/staff");
        Assert.Equal(HttpStatusCode.Unauthorized, listRes.StatusCode);

        var createRes = await client.PostAsJsonAsync("/api/admin/staff", new CreateStaffUserRequest("Doctor Test", "doc@test.com", "0770000000", UserRole.Doctor));
        Assert.Equal(HttpStatusCode.Unauthorized, createRes.StatusCode);

        var auditRes = await client.GetAsync("/api/admin/staff/audit-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, auditRes.StatusCode);
    }

    [Theory]
    [InlineData(UserRole.Patient)]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.FrontDeskStaff)]
    public async Task StaffEndpoints_NonAdmin_Return403(UserRole role)
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), role);

        var listRes = await client.GetAsync("/api/admin/staff");
        Assert.Equal(HttpStatusCode.Forbidden, listRes.StatusCode);

        var createRes = await client.PostAsJsonAsync("/api/admin/staff", new CreateStaffUserRequest("Doctor Test", $"doc-{Guid.NewGuid():N}@test.com", "0770000000", UserRole.Doctor));
        Assert.Equal(HttpStatusCode.Forbidden, createRes.StatusCode);

        var patchRoleRes = await client.PatchAsJsonAsync($"/api/admin/staff/{Guid.NewGuid()}/role", new UpdateStaffRoleRequest(UserRole.Doctor));
        Assert.Equal(HttpStatusCode.Forbidden, patchRoleRes.StatusCode);

        var patchStatusRes = await client.PatchAsJsonAsync($"/api/admin/staff/{Guid.NewGuid()}/status", new UpdateStaffStatusRequest(false));
        Assert.Equal(HttpStatusCode.Forbidden, patchStatusRes.StatusCode);

        var auditRes = await client.GetAsync("/api/admin/staff/audit-logs");
        Assert.Equal(HttpStatusCode.Forbidden, auditRes.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateStaff_AssignRole_AndAuditLogRecorded()
    {
        var adminId = Guid.NewGuid();
        var adminClient = _factory.CreateAuthenticatedClient(adminId, UserRole.Admin);

        var email = $"staff-{Guid.NewGuid():N}@hospital.local";
        var createRequest = new CreateStaffUserRequest("Front Desk User", email, "0771234567", UserRole.FrontDeskStaff);

        var createRes = await adminClient.PostAsJsonAsync("/api/admin/staff", createRequest);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var createdStaff = await createRes.Content.ReadFromJsonAsync<StaffUserDto>(JsonOptions);
        Assert.NotNull(createdStaff);
        Assert.Equal(email, createdStaff.Email);
        Assert.Equal(UserRole.FrontDeskStaff, createdStaff.Role);
        Assert.True(createdStaff.MustChangePassword);

        // Update Role
        var roleRes = await adminClient.PatchAsJsonAsync($"/api/admin/staff/{createdStaff.Id}/role", new UpdateStaffRoleRequest(UserRole.Doctor));
        Assert.Equal(HttpStatusCode.OK, roleRes.StatusCode);
        var updatedRole = await roleRes.Content.ReadFromJsonAsync<StaffUserDto>(JsonOptions);
        Assert.Equal(UserRole.Doctor, updatedRole!.Role);

        // Verify in Audit Logs
        var auditRes = await adminClient.GetAsync("/api/admin/staff/audit-logs");
        Assert.Equal(HttpStatusCode.OK, auditRes.StatusCode);
        var auditLogs = await auditRes.Content.ReadFromJsonAsync<AuditLogListResponse>(JsonOptions);
        Assert.NotNull(auditLogs);
        Assert.Contains(auditLogs.Items, a => a.TargetUserId == createdStaff.Id && a.Action == "StaffRoleUpdated");
    }

    [Fact]
    public async Task Admin_SelfRoleChange_AndSelfDeactivation_Return400()
    {
        var adminId = Guid.NewGuid();
        var adminClient = _factory.CreateAuthenticatedClient(adminId, UserRole.Admin);

        // Self role change
        var roleRes = await adminClient.PatchAsJsonAsync($"/api/admin/staff/{adminId}/role", new UpdateStaffRoleRequest(UserRole.Doctor));
        Assert.Equal(HttpStatusCode.BadRequest, roleRes.StatusCode);

        // Self deactivation
        var statusRes = await adminClient.PatchAsJsonAsync($"/api/admin/staff/{adminId}/status", new UpdateStaffStatusRequest(false));
        Assert.Equal(HttpStatusCode.BadRequest, statusRes.StatusCode);
    }

    [Fact]
    public async Task Admin_DemoteOrDeactivate_LastActiveAdmin_Return400()
    {
        var actorAdminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();

        var actorClient = _factory.CreateAuthenticatedClient(actorAdminId, UserRole.Admin);
        var targetClient = _factory.CreateAuthenticatedClient(targetAdminId, UserRole.Admin);

        // In DB, set any other admin's role to FrontDeskStaff so targetAdmin is the only active Admin
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var otherAdmins = db.Users.Where(u => u.Role == UserRole.Admin && u.Id != targetAdminId).ToList();
            foreach (var o in otherAdmins)
            {
                o.Role = UserRole.FrontDeskStaff;
            }
            db.SaveChanges();
        }

        // Try to demote targetAdmin
        var roleRes = await actorClient.PatchAsJsonAsync($"/api/admin/staff/{targetAdminId}/role", new UpdateStaffRoleRequest(UserRole.Doctor));
        Assert.Equal(HttpStatusCode.BadRequest, roleRes.StatusCode);

        // Try to deactivate targetAdmin
        var statusRes = await actorClient.PatchAsJsonAsync($"/api/admin/staff/{targetAdminId}/status", new UpdateStaffStatusRequest(false));
        Assert.Equal(HttpStatusCode.BadRequest, statusRes.StatusCode);
    }

    [Fact]
    public async Task DeactivatingStaff_InvalidatesExistingTokenImmediately()
    {
        var adminId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var adminClient = _factory.CreateAuthenticatedClient(adminId, UserRole.Admin);
        var staffClient = _factory.CreateAuthenticatedClient(staffId, UserRole.Doctor);

        // Staff client can access an authorized endpoint initially
        var initialRes = await staffClient.GetAsync("/api/appointments?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, initialRes.StatusCode);

        // Admin deactivates staff account
        var deactRes = await adminClient.PatchAsJsonAsync($"/api/admin/staff/{staffId}/status", new UpdateStaffStatusRequest(false));
        Assert.Equal(HttpStatusCode.OK, deactRes.StatusCode);

        // Staff client's token must now be rejected immediately!
        var afterDeactRes = await staffClient.GetAsync("/api/appointments?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.Unauthorized, afterDeactRes.StatusCode);
    }

    [Fact]
    public async Task ForcePasswordReset_InvalidatesExistingToken_AndUserCanChangePassword()
    {
        var adminId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var adminClient = _factory.CreateAuthenticatedClient(adminId, UserRole.Admin);
        var staffClient = _factory.CreateAuthenticatedClient(staffId, UserRole.Doctor);

        // Force password reset
        var resetRes = await adminClient.PostAsJsonAsync($"/api/admin/staff/{staffId}/force-password-reset", new ForcePasswordResetRequest("TempPass!999"));
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);

        var resetData = await resetRes.Content.ReadFromJsonAsync<ForcePasswordResetResponse>(JsonOptions);
        Assert.NotNull(resetData);
        Assert.Equal("TempPass!999", resetData.TemporaryPassword);

        // Existing token fails
        var oldTokenRes = await staffClient.GetAsync("/api/appointments?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenRes.StatusCode);

        // User logs in with temporary password
        var unauthClient = _factory.CreateClient();
        var loginRes = await unauthClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(resetData.Email, "TempPass!999"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginAuth = await loginRes.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(loginAuth);
        Assert.True(loginAuth.User.MustChangePassword);

        // User changes password via /api/auth/change-password
        var changeClient = _factory.CreateClient();
        changeClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginAuth.Token);

        var changeRes = await changeClient.PostAsJsonAsync("/api/auth/change-password", new ChangePasswordRequest("TempPass!999", "PermPass!123", "PermPass!123"));
        Assert.Equal(HttpStatusCode.OK, changeRes.StatusCode);
        var changeAuth = await changeRes.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(changeAuth);
        Assert.False(changeAuth.User.MustChangePassword);

        // New token works for authorized calls
        var newClient = _factory.CreateClient();
        newClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", changeAuth.Token);
        var successRes = await newClient.GetAsync("/api/appointments?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, successRes.StatusCode);
    }
}
