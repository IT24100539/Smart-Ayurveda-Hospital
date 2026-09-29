using Hospital.Application.Abstractions;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_OpensAPatientRecordForTheSameEmail()
    {
        var users = new FakeUsers();
        var patients = new FakePatients();
        var sut = CreateSut(users, patients);

        var result = await sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None);

        Assert.Equal(UserRole.Patient, result.User.Role);
        var patient = Assert.Single(patients.Items);
        Assert.Equal("nimal@example.com", patient.Email);
        Assert.Equal("Nimal", patient.FirstName);
        Assert.Equal("Silva", patient.LastName);
        Assert.Equal("0771234567", patient.Phone);
        Assert.Equal(new DateOnly(1992, 3, 4), patient.DateOfBirth);
        Assert.Equal(Gender.Male, patient.Gender);
        Assert.Equal("SAH-2026-00042", patient.Uhid);
        Assert.Equal(DoshaType.None, patient.Prakriti);
    }

    [Fact]
    public async Task RegisterAsync_WhenPhoneBelongsToAnotherPatient_ThrowsConflict()
    {
        var users = new FakeUsers();
        var patients = new FakePatients();
        patients.Items.Add(new Patient
        {
            Phone = "0771234567",
            Email = "other@example.com",
            Uhid = "SAH-2026-00001"
        });
        var sut = CreateSut(users, patients);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None));
        Assert.Empty(users.Items);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task RegisterAsync_LinksAnExistingChartThatHasNoEmail()
    {
        var patients = new FakePatients();
        var existing = new Patient
        {
            Phone = "0771234567",
            Email = null,
            Uhid = "SAH-2026-00001",
            FirstName = "Nimal"
        };
        patients.Items.Add(existing);
        var sut = CreateSut(new FakeUsers(), patients);

        await sut.RegisterAsync(NewRequest("Nimal Silva"), actorRole: null, CancellationToken.None);

        Assert.Equal("nimal@example.com", existing.Email);
        Assert.Single(patients.Items);
    }

    private static AuthService CreateSut(FakeUsers users, FakePatients patients) =>
        new(users, patients, new FakeUhid(), new FakeHasher(), new FakeJwt(), new FakeUnitOfWork());

    private static RegisterRequest NewRequest(string fullName) => new(
        fullName,
        "nimal@example.com",
        "0771234567",
        "ChangeMe!Patient1",
        DateOfBirth: new DateOnly(1992, 3, 4),
        Gender: Gender.Male);

    private sealed class FakeUhid : IUhidGenerator
    {
        public Task<string> NextAsync(CancellationToken cancellationToken) => Task.FromResult("SAH-2026-00042");
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed";
        public bool Verify(string password, string hash) => hash == "hashed";
    }

    private sealed class FakeJwt : IJwtTokenService
    {
        public (string Token, DateTimeOffset ExpiresAt) Create(User user) =>
            ("token", DateTimeOffset.UtcNow.AddHours(1));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> Items { get; } = new();

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Email == email));

        public Task<User?> FindActiveByRoleAsync(UserRole role, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Role == role && x.IsActive));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Items.Add(user);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePatients : IPatientRepository
    {
        public List<Patient> Items { get; } = new();

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
}
