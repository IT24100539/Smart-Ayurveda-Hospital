using Hospital.Application.Abstractions;
using Hospital.Application.Wards;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class WardServiceTests
{
    [Fact]
    public async Task DecideAdmission_WhenNoFreeBeds_ThrowsWardFullException()
    {
        var wardId = Guid.NewGuid();
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Nimali" };
        var ar = new AdmissionRequest { Id = Guid.NewGuid(), PatientId = patient.Id, WardId = wardId, Status = AdmissionRequestStatus.Pending };

        var ward = new Ward { Id = wardId, Name = "Female", TotalCapacity = 2 };
        ward.Beds.Add(new Bed { Id = Guid.NewGuid(), BedLabel = "A-01", IsOccupied = true });
        ward.Beds.Add(new Bed { Id = Guid.NewGuid(), BedLabel = "A-02", IsOccupied = true });

        var repo = new FakeWardRepository(ward, ar);
        var sut = new WardService(repo, new FakePatients(patient), new FakeUnitOfWork());

        var req = new AdmissionDecisionRequest(true, Guid.NewGuid());

        await Assert.ThrowsAsync<WardFullException>(() => sut.DecideAdmissionAsync(ar.Id, req, CancellationToken.None));

        // Ensure state unchanged
        var stored = await repo.GetAdmissionRequestByIdAsync(ar.Id, CancellationToken.None);
        Assert.Equal(AdmissionRequestStatus.Pending, stored!.Status);
        Assert.Null(stored.BedId);
        // beds remain occupied
        var w = await repo.GetWithBedsAsync(wardId, CancellationToken.None);
        Assert.All(w!.Beds, b => Assert.True(b.IsOccupied));
    }

    [Fact]
    public async Task PatientFacingOccupancy_DoesNotExposeOtherPatientsBedAssignment()
    {
        var wardId = Guid.NewGuid();
        var ward = new Ward { Id = wardId, Name = "Female", TotalCapacity = 2 };
        ward.Beds.Add(new Bed { Id = Guid.NewGuid(), BedLabel = "A-01", IsOccupied = true });
        ward.Beds.Add(new Bed { Id = Guid.NewGuid(), BedLabel = "A-02", IsOccupied = false });

        var repo = new FakeWardRepository(ward, null);
        var sut = new WardService(repo, new FakePatients(new Patient { Id = Guid.NewGuid() }), new FakeUnitOfWork());

        var dto = await sut.GetOccupancyAsync(wardId, forPatient: true, CancellationToken.None);
        Assert.NotNull(dto);
        // Patient-facing DTO must not expose bed-level assignments
        Assert.Empty(dto!.Beds);
        Assert.Equal(2, dto.TotalCapacity);
        Assert.Equal(1, dto.OccupiedBeds);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakePatients : IPatientRepository
    {
        private readonly Patient _p;
        public FakePatients(Patient p) => _p = p;
        public Task<Patient?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult(string.Equals(_p.Email, email, StringComparison.OrdinalIgnoreCase) ? _p : null);
        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(id == _p.Id ? _p : null as Patient);
        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) => Task.FromResult<Patient?>(null);
        public Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult(((IReadOnlyList<Patient>)Array.Empty<Patient>(), 0));
        public Task AddAsync(Patient patient, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeWardRepository : IWardRepository
    {
        private readonly Ward? _ward;
        private readonly AdmissionRequest? _ar;
        public FakeWardRepository(Ward? ward, AdmissionRequest? ar)
        {
            _ward = ward;
            _ar = ar;
        }

        public Task<Ward?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_ward != null && _ward.Id == id ? _ward : null as Ward);
        public Task<Ward?> GetWithBedsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_ward != null && _ward.Id == id ? _ward : null as Ward);
        public Task<IReadOnlyList<Ward>> ListAllAsync(CancellationToken cancellationToken) => Task.FromResult(((IReadOnlyList<Ward>)(_ward != null ? new[] { _ward } : Array.Empty<Ward>())));
        public Task AddAdmissionRequestAsync(AdmissionRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<AdmissionRequest?> GetAdmissionRequestByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_ar != null && _ar.Id == id ? _ar : null as AdmissionRequest);
        public Task<IReadOnlyList<AdmissionRequest>> ListPendingAdmissionsAsync(CancellationToken cancellationToken) => Task.FromResult(((IReadOnlyList<AdmissionRequest>)(_ar != null ? new[] { _ar } : Array.Empty<AdmissionRequest>())));
        public Task<bool> TryApproveAdmissionAssignBedAsync(Guid admissionRequestId, Guid decidedBy, DateTimeOffset decidedAt, CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
