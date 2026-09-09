using Hospital.Application.Abstractions;
using Hospital.Application.Patients;
using Hospital.Application.Patients.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class PatientServiceTests
{
    [Fact]
    public async Task CreateAsync_AssignsUhid_AndPersists()
    {
        var patients = new FakePatientRepository();
        var sut = new PatientService(patients, new FakeUhidGenerator(), new FakeUnitOfWork());

        var result = await sut.CreateAsync(NewCreateRequest(), CancellationToken.None);

        Assert.Equal("SAH-2026-00001", result.Uhid);
        Assert.Equal("Meera", result.FirstName);
        Assert.Single(patients.Items);
    }

    [Fact]
    public async Task CreateAsync_WhenPhoneExists_ThrowsConflict()
    {
        var patients = new FakePatientRepository();
        patients.Items.Add(new Patient { Phone = "9999999999", Uhid = "SAH-2026-00001" });
        var sut = new PatientService(patients, new FakeUhidGenerator(), new FakeUnitOfWork());

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(NewCreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        var sut = new PatientService(new FakePatientRepository(), new FakeUhidGenerator(), new FakeUnitOfWork());
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    private static CreatePatientRequest NewCreateRequest() => new(
        "Meera",
        "Iyer",
        new DateOnly(1988, 4, 12),
        Gender.Female,
        "9999999999",
        "meera@example.com",
        "Kochi",
        "O+",
        null,
        DoshaType.Pitta | DoshaType.Kapha,
        DoshaType.Pitta);

    private sealed class FakeUhidGenerator : IUhidGenerator
    {
        public Task<string> NextAsync(CancellationToken cancellationToken) => Task.FromResult("SAH-2026-00001");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakePatientRepository : IPatientRepository
    {
        public List<Patient> Items { get; } = new();

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Phone == phone));

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
