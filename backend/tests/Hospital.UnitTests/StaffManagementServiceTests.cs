using Hospital.Application.Abstractions;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Dtos;
using Hospital.Application.StaffManagement;
using Hospital.Application.StaffManagement.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;
using Xunit;

namespace Hospital.UnitTests;

public sealed class StaffManagementServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeAuditLogRepository _auditLogs = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeJwtTokenService _jwt = new();
    private readonly StaffManagementService _service;
    private readonly AuthService _authService;

    public StaffManagementServiceTests()
    {
        _service = new StaffManagementService(_users, _auditLogs, _hasher, _uow);
        _authService = new AuthService(_users, new FakePatientRepository(), new FakeUhidGenerator(), _hasher, _jwt, _uow);
    }

    [Fact]
    public async Task CreateStaff_SetsMustChangePassword_AndAuditLogs()
    {
        var adminId = Guid.NewGuid();
        var request = new CreateStaffUserRequest("Doctor Alice", "alice@hospital.local", "0711111111", UserRole.Doctor);

        var result = await _service.CreateStaffAsync(request, adminId, "admin@hospital.local", CancellationToken.None);

        Assert.Equal("Doctor Alice", result.FullName);
        Assert.Equal(UserRole.Doctor, result.Role);
        Assert.True(result.MustChangePassword);
        Assert.True(result.IsActive);
        Assert.Contains(_auditLogs.Items, a => a.Action == "StaffCreated" && a.TargetEmail == "alice@hospital.local");
    }

    [Fact]
    public async Task CreateStaff_PatientRole_ThrowsBadRequest()
    {
        var adminId = Guid.NewGuid();
        var request = new CreateStaffUserRequest("Patient Bob", "bob@hospital.local", "0711111111", UserRole.Patient);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateStaffAsync(request, adminId, "admin@hospital.local", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRole_SelfChange_ThrowsBadRequest()
    {
        var adminId = Guid.NewGuid();
        var admin = new User { Id = adminId, Email = "admin@hospital.local", Role = UserRole.Admin, IsActive = true };
        await _users.AddAsync(admin, CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateRoleAsync(adminId, new UpdateStaffRoleRequest(UserRole.Doctor), adminId, admin.Email, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRole_LastActiveAdmin_ThrowsBadRequest()
    {
        var actorAdminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();

        // Only one active admin in repo
        var targetAdmin = new User { Id = targetAdminId, Email = "target@hospital.local", Role = UserRole.Admin, IsActive = true };
        await _users.AddAsync(targetAdmin, CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateRoleAsync(targetAdminId, new UpdateStaffRoleRequest(UserRole.FrontDeskStaff), actorAdminId, "actor@hospital.local", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRole_IncrementsTokenVersion_WhenSuccessful()
    {
        var actorAdminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var staff = new User { Id = targetId, Email = "staff@hospital.local", Role = UserRole.FrontDeskStaff, TokenVersion = 1, IsActive = true };
        await _users.AddAsync(staff, CancellationToken.None);

        var updated = await _service.UpdateRoleAsync(targetId, new UpdateStaffRoleRequest(UserRole.Doctor), actorAdminId, "actor@hospital.local", CancellationToken.None);

        Assert.Equal(UserRole.Doctor, updated.Role);
        Assert.Equal(2, staff.TokenVersion);
        Assert.Contains(_auditLogs.Items, a => a.Action == "StaffRoleUpdated" && a.TargetUserId == targetId);
    }

    [Fact]
    public async Task UpdateStatus_SelfDeactivation_ThrowsBadRequest()
    {
        var adminId = Guid.NewGuid();
        var admin = new User { Id = adminId, Email = "admin@hospital.local", Role = UserRole.Admin, IsActive = true };
        await _users.AddAsync(admin, CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateStatusAsync(adminId, new UpdateStaffStatusRequest(false), adminId, admin.Email, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatus_LastActiveAdmin_ThrowsBadRequest()
    {
        var actorAdminId = Guid.NewGuid();
        var targetAdminId = Guid.NewGuid();

        var targetAdmin = new User { Id = targetAdminId, Email = "target@hospital.local", Role = UserRole.Admin, IsActive = true };
        await _users.AddAsync(targetAdmin, CancellationToken.None);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateStatusAsync(targetAdminId, new UpdateStaffStatusRequest(false), actorAdminId, "actor@hospital.local", CancellationToken.None));
    }

    [Fact]
    public async Task UpdateStatus_Deactivate_IncrementsTokenVersion()
    {
        var actorAdminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var staff = new User { Id = targetId, Email = "staff@hospital.local", Role = UserRole.Doctor, TokenVersion = 1, IsActive = true };
        await _users.AddAsync(staff, CancellationToken.None);

        var updated = await _service.UpdateStatusAsync(targetId, new UpdateStaffStatusRequest(false), actorAdminId, "actor@hospital.local", CancellationToken.None);

        Assert.False(updated.IsActive);
        Assert.False(staff.IsActive);
        Assert.Equal(2, staff.TokenVersion);
        Assert.Contains(_auditLogs.Items, a => a.Action == "StaffDeactivated");
    }

    [Fact]
    public async Task ForcePasswordReset_IncrementsTokenVersion_AndSetsMustChangePassword()
    {
        var actorAdminId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var staff = new User { Id = targetId, Email = "staff@hospital.local", Role = UserRole.Doctor, TokenVersion = 1, MustChangePassword = false };
        await _users.AddAsync(staff, CancellationToken.None);

        var response = await _service.ForcePasswordResetAsync(targetId, new ForcePasswordResetRequest("NewTempPass!123"), actorAdminId, "admin@hospital.local", CancellationToken.None);

        Assert.Equal("NewTempPass!123", response.TemporaryPassword);
        Assert.True(staff.MustChangePassword);
        Assert.Equal(2, staff.TokenVersion);
        Assert.Contains(_auditLogs.Items, a => a.Action == "StaffPasswordResetForced");
    }

    [Fact]
    public async Task ChangePassword_ClearsMustChangePassword_AndIncrementsTokenVersion()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "staff@hospital.local",
            PasswordHash = _hasher.Hash("OldPass!123"),
            MustChangePassword = true,
            TokenVersion = 1,
            IsActive = true
        };
        await _users.AddAsync(user, CancellationToken.None);

        var response = await _authService.ChangePasswordAsync(userId, new ChangePasswordRequest("OldPass!123", "NewPass!456", "NewPass!456"), CancellationToken.None);

        Assert.False(user.MustChangePassword);
        Assert.Equal(2, user.TokenVersion);
        Assert.False(response.User.MustChangePassword);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; } = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindActiveByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Role == role && x.IsActive));

        public Task<IReadOnlyList<User>> ListStaffAsync(string? query, UserRole? role, bool? isActive, int skip, int take, CancellationToken cancellationToken)
        {
            var q = Items.Where(x => x.Role != UserRole.Patient);
            if (role.HasValue) q = q.Where(x => x.Role == role.Value);
            if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);
            IReadOnlyList<User> list = q.Skip(skip).Take(take).ToList();
            return Task.FromResult(list);
        }

        public Task<int> CountStaffAsync(string? query, UserRole? role, bool? isActive, CancellationToken cancellationToken)
        {
            var q = Items.Where(x => x.Role != UserRole.Patient);
            if (role.HasValue) q = q.Where(x => x.Role == role.Value);
            if (isActive.HasValue) q = q.Where(x => x.IsActive == isActive.Value);
            return Task.FromResult(q.Count());
        }

        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Items.Count(x => x.Role == UserRole.Admin && x.IsActive));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> Items { get; } = new();

        public Task AddAsync(AuditLog log, CancellationToken cancellationToken)
        {
            Items.Add(log);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<AuditLog> Items, int Total)> ListAsync(int skip, int take, CancellationToken cancellationToken)
        {
            IReadOnlyList<AuditLog> list = Items.Skip(skip).Take(take).ToList();
            return Task.FromResult((list, Items.Count));
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<Patient?>(null);
        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) => Task.FromResult<Patient?>(null);
        public Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken) => Task.FromResult<Patient?>(null);
        public Task<Patient?> GetByUhidAsync(string uhid, CancellationToken cancellationToken) => Task.FromResult<Patient?>(null);
        public Task AddAsync(Patient patient, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Patient>)new List<Patient>(), 0));
    }

    private sealed class FakeUhidGenerator : IUhidGenerator
    {
        public Task<string> NextAsync(CancellationToken cancellationToken) => Task.FromResult("UHID-1001");
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public (string Token, DateTimeOffset ExpiresAt) Create(User user) => ("fake-token", DateTimeOffset.UtcNow.AddHours(2));
    }
}
