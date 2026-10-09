using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Communication;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;

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
            new FixedClock(),
            NullPatientEventNotifier.Instance);

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
            new FixedClock(),
            NullPatientEventNotifier.Instance);

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
            new FixedClock(),
            NullPatientEventNotifier.Instance);

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
            new FixedClock(),
            NullPatientEventNotifier.Instance);

        // Owner can cancel
        await sut.CancelAsync(appt.Id, owner.Id, CancellationToken.None);
        var stored = await appointments.GetByIdAsync(appt.Id, CancellationToken.None);
        Assert.Equal(AppointmentStatus.Cancelled, stored!.Status);

        // Other cannot cancel
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CancelAsync(appt.Id, other.Id, CancellationToken.None));
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
            new FixedClock(),
            NullPatientEventNotifier.Instance);

        var request = new CreateAppointmentRequest(
            patient.Id,
            treatment.Id,
            schedule.Id,
            new DateOnly(2026, 9, 15),
            "09:00-10:00");

        await Assert.ThrowsAsync<DomainException>(() => sut.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task RescheduleAsync_WithinCapacity_UpdatesAppointment()
    {
        var patient = new Patient { Id = Guid.NewGuid(), Uhid = "U-1", FirstName = "A", LastName = "B" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "T", DurationMinutes = 60 };
        var originalSchedule = new TreatmentSchedule
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            DayOfWeek = DayOfWeek.Tuesday,
            TimeSlot = "09:00-10:00",
            MaxPatients = 5,
            IsActive = true
        };
        var newSchedule = new TreatmentSchedule
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeSlot = "10:00-11:00",
            MaxPatients = 5,
            IsActive = true
        };

        var appt = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            Patient = patient,
            TreatmentId = treatment.Id,
            Treatment = treatment,
            ScheduleId = originalSchedule.Id,
            RequestedDate = new DateOnly(2026, 9, 15), // Tuesday
            RequestedTimeSlot = "09:00-10:00",
            Status = AppointmentStatus.Approved
        };

        var repo = new FakeAppointmentRepository();
        repo.Store(appt);
        repo.ActiveCount = 1;

        var treatments = new FakeTreatments(treatment, newSchedule);
        var sut = new AppointmentService(
            repo,
            new FakePatients(patient),
            treatments,
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock(),
            NullPatientEventNotifier.Instance);

        var result = await sut.RescheduleAsync(
            appt.Id,
            new RescheduleAppointmentRequest(new DateOnly(2026, 9, 16), "10:00-11:00", newSchedule.Id),
            patient.Id,
            CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 9, 16), result.RequestedDate);
        Assert.Equal("10:00-11:00", result.RequestedTimeSlot);
        Assert.Equal(newSchedule.Id, result.ScheduleId);
    }

    [Fact]
    public async Task RescheduleAsync_OverCapacity_ThrowsSlotFullException()
    {
        var patient = new Patient { Id = Guid.NewGuid(), Uhid = "U-1", FirstName = "A", LastName = "B" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "T", DurationMinutes = 60 };
        var schedule = new TreatmentSchedule
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeSlot = "10:00-11:00",
            MaxPatients = 2,
            IsActive = true
        };

        var appt = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            Patient = patient,
            TreatmentId = treatment.Id,
            Treatment = treatment,
            ScheduleId = schedule.Id,
            RequestedDate = new DateOnly(2026, 9, 15),
            RequestedTimeSlot = "09:00-10:00",
            Status = AppointmentStatus.Approved
        };

        var repo = new FakeAppointmentRepository();
        repo.Store(appt);
        repo.ActiveCount = 2; // Full!

        var sut = new AppointmentService(
            repo,
            new FakePatients(patient),
            new FakeTreatments(treatment, schedule),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock(),
            NullPatientEventNotifier.Instance);

        await Assert.ThrowsAsync<ConflictException>(() => sut.RescheduleAsync(
            appt.Id,
            new RescheduleAppointmentRequest(new DateOnly(2026, 9, 16), "10:00-11:00", schedule.Id),
            patient.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task RescheduleAsync_WhenCalledByNonOwner_ThrowsUnauthorizedException()
    {
        var owner = new Patient { Id = Guid.NewGuid(), Uhid = "U-1", FirstName = "A", LastName = "B" };
        var other = new Patient { Id = Guid.NewGuid(), Uhid = "U-2", FirstName = "C", LastName = "D" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "T", DurationMinutes = 60 };
        var schedule = new TreatmentSchedule
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeSlot = "10:00-11:00",
            MaxPatients = 5,
            IsActive = true
        };

        var appt = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = owner.Id,
            Patient = owner,
            TreatmentId = treatment.Id,
            Treatment = treatment,
            ScheduleId = schedule.Id,
            RequestedDate = new DateOnly(2026, 9, 15),
            RequestedTimeSlot = "09:00-10:00",
            Status = AppointmentStatus.Approved
        };

        var repo = new FakeAppointmentRepository();
        repo.Store(appt);

        var sut = new AppointmentService(
            repo,
            new FakePatients(owner),
            new FakeTreatments(treatment, schedule),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock(),
            NullPatientEventNotifier.Instance);

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.RescheduleAsync(
            appt.Id,
            new RescheduleAppointmentRequest(new DateOnly(2026, 9, 16), "10:00-11:00", schedule.Id),
            requestingPatientId: other.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task StatusChanges_StoreEventsForTheOwningPatientOnly()
    {
        var owner = new Patient { Id = Guid.NewGuid(), Uhid = "SAH-2026-00041", FirstName = "Leela", LastName = "Menon" };
        var other = new Patient { Id = Guid.NewGuid(), Uhid = "SAH-2026-00042", FirstName = "Arun", LastName = "Nair" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga" };
        var schedule = new TreatmentSchedule
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            DayOfWeek = DayOfWeek.Wednesday,
            TimeSlot = "10:00-11:00",
            MaxPatients = 5,
            IsActive = true
        };
        var approved = Visit(owner, treatment, AppointmentStatus.Pending);
        var rejected = Visit(owner, treatment, AppointmentStatus.Pending);
        var moved = Visit(owner, treatment, AppointmentStatus.Approved);
        var cancelled = Visit(owner, treatment, AppointmentStatus.Pending);
        var completed = Visit(owner, treatment, AppointmentStatus.Approved);
        var repo = new FakeAppointmentRepository();
        repo.Store(approved);
        repo.Store(rejected);
        repo.Store(moved);
        repo.Store(cancelled);
        repo.Store(completed);

        var notifications = new InMemoryNotificationRepository();
        var tokens = new InMemoryDeviceTokenRepository();
        await tokens.AddAsync(new PatientDeviceToken { PatientId = owner.Id, Token = "owner-phone", Platform = "android" }, CancellationToken.None);
        await tokens.AddAsync(new PatientDeviceToken { PatientId = other.Id, Token = "other-phone", Platform = "android" }, CancellationToken.None);
        var push = new RecordingPushSender();
        var events = new PatientEventNotifier(
            notifications,
            tokens,
            push,
            new NoopUnitOfWork(),
            NullLogger<PatientEventNotifier>.Instance);
        var sut = new AppointmentService(
            repo,
            new FakePatients(owner),
            new FakeTreatments(treatment, schedule),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock(),
            events);

        await sut.UpdateStatusAsync(approved.Id, new UpdateAppointmentStatusRequest(AppointmentStatus.Approved, null), CancellationToken.None);
        await sut.UpdateStatusAsync(rejected.Id, new UpdateAppointmentStatusRequest(AppointmentStatus.Rejected, null), CancellationToken.None);
        await sut.RescheduleAsync(
            moved.Id,
            new RescheduleAppointmentRequest(new DateOnly(2026, 9, 16), "10:00-11:00", schedule.Id),
            owner.Id,
            CancellationToken.None);
        await sut.CancelAsync(cancelled.Id, owner.Id, CancellationToken.None);

        var beforeComplete = notifications.Items.Count;
        await sut.UpdateStatusAsync(completed.Id, new UpdateAppointmentStatusRequest(AppointmentStatus.Completed, null), CancellationToken.None);
        Assert.Equal(beforeComplete, notifications.Items.Count);

        Assert.Equal(4, notifications.Items.Count);
        Assert.All(notifications.Items, item =>
        {
            Assert.Equal(owner.Id, item.PatientId);
            Assert.Null(item.StaffUserId);
            Assert.Contains("Abhyanga", item.Message);
        });
        Assert.Contains(notifications.Items, item => item.Type == NotificationType.AppointmentApproved);
        Assert.Contains(notifications.Items, item => item.Type == NotificationType.AppointmentRejected);
        Assert.Contains(notifications.Items, item => item.Type == NotificationType.AppointmentRescheduled);
        Assert.Contains(notifications.Items, item => item.Type == NotificationType.AppointmentCancelled);
        Assert.Equal(4, push.Sent.Count);
        Assert.All(push.Sent, sent =>
        {
            Assert.Equal(owner.Id, sent.PatientId);
            Assert.Equal(new[] { "owner-phone" }, sent.DeviceTokens);
        });

        var inbox = new NotificationService(notifications, new ScriptActor(owner), new NoopUnitOfWork());
        Assert.Equal(4, (await inbox.GetForPatient(owner.Id, CancellationToken.None)).Count);
        await Assert.ThrowsAsync<ForbiddenException>(() => inbox.GetForPatient(other.Id, CancellationToken.None));

        var stranger = new NotificationService(notifications, new ScriptActor(other), new NoopUnitOfWork());
        await Assert.ThrowsAsync<ForbiddenException>(() => stranger.GetForPatient(owner.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => stranger.MarkReadAsync(notifications.Items[0].Id, CancellationToken.None));
        Assert.Empty(await stranger.GetForPatient(other.Id, CancellationToken.None));
    }

    [Fact]
    public async Task Approve_StoresTheEvent_WhenPushFails()
    {
        var owner = new Patient { Id = Guid.NewGuid(), Uhid = "SAH-2026-00043", FirstName = "Leela", LastName = "Menon" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Shirodhara" };
        var appointment = Visit(owner, treatment, AppointmentStatus.Pending);
        var repo = new FakeAppointmentRepository();
        repo.Store(appointment);
        var notifications = new InMemoryNotificationRepository();
        var tokens = new InMemoryDeviceTokenRepository();
        await tokens.AddAsync(new PatientDeviceToken { PatientId = owner.Id, Token = "owner-phone", Platform = "ios" }, CancellationToken.None);
        var events = new PatientEventNotifier(
            notifications,
            tokens,
            new ThrowingPushSender(),
            new NoopUnitOfWork(),
            NullLogger<PatientEventNotifier>.Instance);
        var sut = new AppointmentService(
            repo,
            new FakePatients(owner),
            new FakeTreatments(treatment),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock(),
            events);

        var result = await sut.UpdateStatusAsync(
            appointment.Id,
            new UpdateAppointmentStatusRequest(AppointmentStatus.Approved, null),
            CancellationToken.None);

        Assert.Equal(AppointmentStatus.Approved, result.Status);
        Assert.Equal(NotificationType.AppointmentApproved, Assert.Single(notifications.Items).Type);
    }

    [Fact]
    public async Task CreateAsync_WhenTreatmentIsInactive_ThrowsDomainException()
    {
        var patient = new Patient { Id = Guid.NewGuid(), FirstName = "Asha", LastName = "Nair" };
        var treatment = new Treatment { Id = Guid.NewGuid(), Name = "Abhyanga", IsActive = false };
        var appointments = new FakeAppointmentRepository();
        var sut = new AppointmentService(
            appointments,
            new FakePatients(patient),
            new FakeTreatments(treatment),
            new FakeBookingValidator(),
            new FakeUnitOfWork(),
            new FixedClock(),
            NullPatientEventNotifier.Instance);

        var request = new CreateAppointmentRequest(
            patient.Id,
            treatment.Id,
            null,
            new DateOnly(2026, 9, 15),
            "09:00-10:00");

        var ex = await Assert.ThrowsAsync<DomainException>(() => sut.CreateAsync(request, CancellationToken.None));
        Assert.Contains("inactive", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Appointment Visit(Patient patient, Treatment treatment, AppointmentStatus status) => new()
    {
        Id = Guid.NewGuid(),
        PatientId = patient.Id,
        Patient = patient,
        TreatmentId = treatment.Id,
        Treatment = treatment,
        RequestedDate = new DateOnly(2026, 9, 15),
        RequestedTimeSlot = "09:00-10:00",
        Status = status
    };

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

        public Task<bool> TryRescheduleWithinCapacityAsync(
            Appointment appointment,
            DateOnly newDate,
            string newTimeSlot,
            Guid? newScheduleId,
            int maxPatients,
            CancellationToken cancellationToken)
        {
            if (ActiveCount >= maxPatients) return Task.FromResult(false);
            appointment.RequestedDate = newDate;
            appointment.RequestedTimeSlot = newTimeSlot;
            appointment.ScheduleId = newScheduleId;
            return Task.FromResult(true);
        }

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

        public Task<Treatment?> GetByIdWithScheduleAsync(Guid id, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);

        public Task<(IReadOnlyList<Treatment> Items, int Total)> SearchAsync(
            string? name,
            TreatmentCategory? category,
            bool? activeOnly,
            int page,
            int pageSize,
            string? sort,
            CancellationToken cancellationToken) =>
            Task.FromResult(((IReadOnlyList<Treatment>)new[] { _treatment }, 1));

        public Task AddAsync(Treatment treatment, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<TreatmentSchedule?> GetScheduleEntryAsync(Guid treatmentId, Guid entryId, CancellationToken cancellationToken) =>
            Task.FromResult(_schedule is not null && _schedule.Id == entryId ? _schedule : null);

        public Task AddScheduleAsync(TreatmentSchedule entry, CancellationToken cancellationToken) => Task.CompletedTask;

        public void RemoveSchedule(TreatmentSchedule entry) { }

        public Task<Therapist?> GetTherapistByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Therapist?>(null);
    }
}
