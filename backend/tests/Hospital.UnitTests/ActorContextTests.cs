using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class ActorContextTests
{
    [Fact]
    public async Task RequirePatientAsync_OpensAChartWhenTheLoginHasNone()
    {
        var user = NewPatientUser();
        var patients = new FakePatients();
        var sut = CreateSut(user, patients);

        var chart = await sut.RequirePatientAsync(CancellationToken.None);

        Assert.Equal("meera.nair@example.local", chart.Email);
        Assert.Equal("Meera", chart.FirstName);
        Assert.Equal("Nair", chart.LastName);
        Assert.Equal("9876500001", chart.Phone);
        Assert.Equal("SAH-2026-00042", chart.Uhid);
        Assert.Equal(Gender.Unspecified, chart.Gender);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task RequirePatientAsync_ReturnsTheExistingChartByEmail()
    {
        var user = NewPatientUser();
        var existing = new Patient
        {
            Email = user.Email,
            FirstName = "Meera",
            LastName = "Nair",
            Phone = "9876500001",
            Uhid = "SAH-2026-00007"
        };
        var patients = new FakePatients();
        patients.Items.Add(existing);
        var sut = CreateSut(user, patients);

        var chart = await sut.RequirePatientAsync(CancellationToken.None);

        Assert.Same(existing, chart);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task RequirePatientAsync_WhenPhoneBelongsToAnotherEmail_DoesNotOpenASecondChart()
    {
        var user = NewPatientUser();
        var patients = new FakePatients();
        patients.Items.Add(new Patient
        {
            Phone = user.PhoneNumber,
            Email = "other@example.local",
            Uhid = "SAH-2026-00001"
        });
        var sut = CreateSut(user, patients);

        var error = await Assert.ThrowsAsync<DomainException>(
            () => sut.RequirePatientAsync(CancellationToken.None));

        Assert.Contains("No patient record is linked", error.Message);
        Assert.Single(patients.Items);
    }

    private static ActorContext CreateSut(User user, FakePatients patients) =>
        new(
            new ScriptCurrentUser(user),
            new FakeUsers { Items = { user } },
            patients,
            new FakeStaff(),
            new FakeUnitOfWork(),
            new FakeUhid());

    private static User NewPatientUser() => new()
    {
        Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        FullName = "Meera Nair",
        Email = "meera.nair@example.local",
        PhoneNumber = "9876500001",
        Role = UserRole.Patient,
        IsActive = true
    };

    private sealed class ScriptCurrentUser : ICurrentUser
    {
        private readonly User _user;

        public ScriptCurrentUser(User user) => _user = user;

        public bool IsAuthenticated => true;
        public Guid UserId => _user.Id;
        public string Email => _user.Email;
        public UserRole Role => _user.Role;
    }

    private sealed class FakeUhid : IUhidGenerator
    {
        public Task<string> NextAsync(CancellationToken cancellationToken) =>
            Task.FromResult("SAH-2026-00042");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<User?> FindActiveByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Role == role && x.IsActive));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<User>> ListStaffAsync(
            string? query, UserRole? role, bool? isActive, int skip, int take, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<User>>([]);

        public Task<int> CountStaffAsync(
            string? query, UserRole? role, bool? isActive, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakePatients : IPatientRepository
    {
        public List<Patient> Items { get; } = [];

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Phone == phone));

        public Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x =>
                string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(
            string? query, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Patient>)Items, Items.Count));

        public Task AddAsync(Patient patient, CancellationToken cancellationToken)
        {
            Items.Add(patient);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStaff : IStaffUserRepository
    {
        public Task<StaffUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<StaffUser?>(null);

        public Task<StaffUser?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<StaffUser?>(null);

        public Task<StaffUser?> FindActiveByRoleAsync(StaffRole role, CancellationToken cancellationToken) =>
            Task.FromResult<StaffUser?>(null);

        public Task<IReadOnlyList<StaffUser>> ListActiveAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StaffUser>>([]);
    }
}
