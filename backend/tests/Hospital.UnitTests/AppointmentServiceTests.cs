using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class AppointmentServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenSlotAlreadyTaken_ThrowsConflict()
    {
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair", Uhid = "SAH-2026-00002" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga" };
        var appointments = new FakeAppointmentRepository { HasActiveSlot = true };
        var sut = new AppointmentService(
            appointments,
            new FakePatients(patient),
            new FakeTreatments(treatment),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock());

        var request = new CreateAppointmentRequest(
            patient.Id,
            treatment.Id,
            null,
            new DateOnly(2026, 9, 15),
            "09:00-10:00");

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenRequestedDateDoesNotMatchSchedule_Throws()
    {
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga" };
        var schedule = new TreatmentSchedule { Id = Guid.NewGuid(), TreatmentId = treatment.Id, TimeSlot = "09:00-10:00", DayOfWeek = DayOfWeek.Monday, IsActive = true };
        var appointments = new FakeAppointmentRepository();
        var sut = new AppointmentService(
            appointments,
            new FakePatients(patient),
            new FakeTreatments(treatment, schedule),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock());

        var request = new CreateAppointmentRequest(
            patient.Id,
            treatment.Id,
            schedule.Id,
            new DateOnly(2026, 9, 15), // Tuesday, so it does not match the Monday schedule
            "09:00-10:00");

        await Assert.ThrowsAsync<DomainException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleAtCapacity_ThrowsConflict()
    {
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga" };
        var schedule = new TreatmentSchedule { Id = Guid.NewGuid(), TreatmentId = treatment.Id, TimeSlot = "09:00-10:00", DayOfWeek = DayOfWeek.Tuesday, IsActive = true, MaxPatients = 2 };
        var appointments = new FakeAppointmentRepository { ActiveCount = 2 };
        var sut = new AppointmentService(
            appointments,
            new FakePatients(patient),
            new FakeTreatments(treatment, schedule),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock());

        var request = new CreateAppointmentRequest(
            patient.Id,
            treatment.Id,
            schedule.Id,
            new DateOnly(2026, 9, 15),
            "09:00-10:00");

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task CancelAsync_AllowsOwnerAndRejectsOthers()
    {
        var owner = new Patient { Id = Guid.NewGuid(), FirstName = "Owner" };
        var other = new Patient { Id = Guid.NewGuid(), FirstName = "Other" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga" };
        var appt = new Appointment { Id = Guid.NewGuid(), PatientId = owner.Id, TreatmentId = treatment.Id, RequestedDate = new DateOnly(2026,9,15), RequestedTimeSlot = "09:00-10:00", Status = AppointmentStatus.Pending };

        var appointments = new FakeAppointmentRepository();
        appointments.Store(appt);

        var sut = new AppointmentService(
            appointments,
            new FakePatients(owner),
            new FakeTreatments(treatment),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock());

        // Owner can cancel
        await sut.CancelAsync(appt.Id, owner.Id, CancellationToken.None);
        var stored = await appointments.GetByIdAsync(appt.Id, CancellationToken.None);
        Assert.Equal(AppointmentStatus.Cancelled, stored!.Status);

        // Other cannot cancel
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.CancelAsync(appt.Id, other.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WhenScheduleBelongsToOtherTreatment_Throws()
    {
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga" };
        var otherTreatmentId = Guid.NewGuid();
        var schedule = new TreatmentSchedule { Id = Guid.NewGuid(), TreatmentId = otherTreatmentId, TimeSlot = "09:00-10:00" };
        var sut = new AppointmentService(
            new FakeAppointmentRepository(),
            new FakePatients(patient),
            new FakeTreatments(treatment, schedule),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock());

        var request = new CreateAppointmentRequest(
            patient.Id,
            treatment.Id,
            schedule.Id,
            new DateOnly(2026, 9, 15),
            "09:00-10:00");

        await Assert.ThrowsAsync<DomainException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 9, 10, 0, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        public bool HasActiveSlot { get; set; }
        public int ActiveCount { get; set; }

        private readonly Dictionary<Guid, Appointment> _store = new();

        public void Store(Appointment a) => _store[a.Id] = a;

        public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_store.TryGetValue(id, out var a) ? a : null as Appointment);

        public Task<(IReadOnlyList<Appointment> Items, int Total)> ListAsync(
            DateOnly? onDate, Guid? patientId, Guid? treatmentId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Appointment>)Array.Empty<Appointment>(), 0));

        public Task<bool> HasActiveSlotAsync(
            Guid patientId,
            Guid treatmentId,
            DateOnly requestedDate,
            string requestedTimeSlot,
            Guid? excludeId,
            CancellationToken cancellationToken) =>
            Task.FromResult(HasActiveSlot);

        public Task<int> CountActiveAppointmentsAsync(Guid treatmentId, DateOnly requestedDate, string requestedTimeSlot, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCount);

        public Task<bool> TryAddWithinCapacityAsync(Appointment appointment, int maxPatients, CancellationToken cancellationToken) =>
            Task.FromResult(ActiveCount < maxPatients);

        public Task AddAsync(Appointment appointment, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeBookingValidator : IBookingValidator
    {
        public void Validate(TreatmentSchedule schedule, DateOnly requestedDate, string requestedTimeSlot)
        {
            if (!schedule.IsActive) throw new DomainException("inactive");
            if (requestedDate.DayOfWeek != schedule.DayOfWeek) throw new DomainException("day mismatch");
            if (!string.Equals(schedule.TimeSlot?.Trim(), requestedTimeSlot?.Trim(), StringComparison.Ordinal)) throw new DomainException("slot mismatch");
        }
    }

    private sealed class FakePatients : IPatientRepository
    {
        private readonly Patient _patient;

        public FakePatients(Patient patient) => _patient = patient;

        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == _patient.Id ? _patient : null);

        public Task<Patient?> GetByPhoneAsync(string phone, CancellationToken cancellationToken) =>
            Task.FromResult<Patient?>(null);

        Task<Patient?> IPatientRepository.GetByEmailAsync(string email, CancellationToken cancellationToken) =>
            Task.FromResult<Patient?>(null);

        public Task<(IReadOnlyList<Patient> Items, int Total)> SearchAsync(
            string? query, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Patient>)Array.Empty<Patient>(), 0));

        public Task AddAsync(Patient patient, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeTreatments : ITreatmentRepository
    {
        public Task<IReadOnlyList<TreatmentSchedule>> ListSchedulesAsync(Guid treatmentId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TreatmentSchedule>>(_schedule is not null && _schedule.TreatmentId == treatmentId
                ? new[] { _schedule } : Array.Empty<TreatmentSchedule>());
        private readonly Treatment _treatment;
        private readonly TreatmentSchedule? _schedule;

        public FakeTreatments(Treatment treatment, TreatmentSchedule? schedule = null)
        {
            _treatment = treatment;
            _schedule = schedule;
        }

        public Task<Treatment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == _treatment.Id ? _treatment : null);

        public Task<TreatmentSchedule?> GetScheduleByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_schedule is not null && _schedule.Id == id ? _schedule : null);

    }
}
