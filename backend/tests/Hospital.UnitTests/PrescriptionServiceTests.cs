using Hospital.Application.Abstractions;
using Hospital.Application.Communication;
using Hospital.Application.Prescriptions;
using Hospital.Application.Prescriptions.Dtos;
using Hospital.Application.Prescriptions.Validators;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hospital.UnitTests;

public sealed class PrescriptionServiceTests
{
    [Fact]
    public void CreateValidator_RejectsBlankMedicineName()
    {
        var validator = new CreatePrescriptionRequestValidator();
        var result = validator.Validate(new CreatePrescriptionRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new[]
            {
                new PrescriptionItemRequest("  ", "3 g", "Twice daily after meals", "14 days", "Warm water")
            }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.Contains(nameof(PrescriptionItemRequest.Name)));
    }

    [Fact]
    public async Task Create_RejectsVisitThatIsNotApprovedOrCompleted()
    {
        var patient = Chart();
        var appointment = Visit(patient.Id, AppointmentStatus.Pending);
        var sut = CreateSut(patient, appointment, out _, out _, out _);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            sut.CreateAsync(Request(patient.Id, appointment.Id, "Ashwagandha churna"), CancellationToken.None));

        Assert.Contains("approved or completed", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PatientA_CannotReadPatientB()
    {
        var patientA = Chart();
        var patientB = Chart();
        var appointment = Visit(patientA.Id, AppointmentStatus.Approved);
        var sut = CreateSut(patientA, appointment, out var current, out _, out var actors);
        var created = await sut.CreateAsync(Request(patientA.Id, appointment.Id, "Ashwagandha churna"), CancellationToken.None);
        await sut.IssueAsync(created.Id, CancellationToken.None);

        current.Role = UserRole.Patient;
        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patientB);

        var error = await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetAsync(created.Id, CancellationToken.None));
        Assert.Contains("your own", error.Message, StringComparison.OrdinalIgnoreCase);

        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patientA);
        var own = await sut.GetAsync(created.Id, CancellationToken.None);
        Assert.Equal(patientA.Id, own.PatientId);
        Assert.Equal("Ashwagandha churna", own.Items[0].Name);
    }

    [Fact]
    public async Task IssuedPrescription_CannotBeEdited_RevisionKeepsThePreviousText()
    {
        var patient = Chart();
        var appointment = Visit(patient.Id, AppointmentStatus.Completed);
        var sut = CreateSut(patient, appointment, out var current, out var store, out _);

        var created = await sut.CreateAsync(Request(patient.Id, appointment.Id, "Ashwagandha churna"), CancellationToken.None);
        Assert.Equal(PrescriptionStatus.Draft, created.Status);

        var updated = await sut.UpdateDraftAsync(
            created.Id,
            new UpdatePrescriptionRequest(new[] { Item("Guduchi ghana") }),
            CancellationToken.None);
        Assert.Equal("Guduchi ghana", updated.Items[0].Name);

        var issued = await sut.IssueAsync(created.Id, CancellationToken.None);
        Assert.Equal(PrescriptionStatus.Issued, issued.Status);
        Assert.NotNull(issued.IssuedAt);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.UpdateDraftAsync(created.Id, new UpdatePrescriptionRequest(new[] { Item("Changed after issue") }), CancellationToken.None));
        Assert.Contains("revision", conflict.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Guduchi ghana", store.Single(item => item.Id == created.Id).Items.Single().Name);

        var revised = await sut.ReviseAsync(
            created.Id,
            new RevisePrescriptionRequest("Dose reduced after nadi pariksha.", new[] { Item("Brahmi ghrita") }),
            CancellationToken.None);

        var previous = store.Single(item => item.Id == created.Id);
        Assert.Equal(PrescriptionStatus.Superseded, previous.Status);
        Assert.Equal("Guduchi ghana", previous.Items.Single().Name);
        Assert.Equal(revised.Id, previous.SupersededByPrescriptionId);
        Assert.Equal(PrescriptionStatus.Issued, revised.Status);
        Assert.Equal(2, revised.RevisionNumber);
        Assert.Equal(created.Id, revised.RevisesPrescriptionId);
        Assert.Equal("Brahmi ghrita", revised.Items[0].Name);

        current.Role = UserRole.Patient;
        var history = await sut.GetHistoryAsync(revised.Id, CancellationToken.None);
        Assert.Equal(2, history.Versions.Count);
        Assert.Equal("Guduchi ghana", history.Versions[0].Items[0].Name);
        Assert.Equal("Brahmi ghrita", history.Versions[1].Items[0].Name);
        var revision = Assert.Single(history.Revisions);
        Assert.Equal("Dose reduced after nadi pariksha.", revision.Reason);
        Assert.Equal(created.Id, revision.PreviousPrescriptionId);
        Assert.Equal(revised.Id, revision.RevisedPrescriptionId);
    }

    [Fact]
    public async Task FrontDesk_CannotReadPrescriptions()
    {
        var patient = Chart();
        var appointment = Visit(patient.Id, AppointmentStatus.Approved);
        var sut = CreateSut(patient, appointment, out var current, out _, out _);
        var created = await sut.CreateAsync(Request(patient.Id, appointment.Id, "Triphala churna"), CancellationToken.None);
        await sut.IssueAsync(created.Id, CancellationToken.None);

        current.Role = UserRole.FrontDeskStaff;
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetAsync(created.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.ListAsync(null, null, 1, 20, CancellationToken.None));
    }

    [Fact]
    public async Task Issue_StoresAnEventForThatPatientOnly()
    {
        var patient = Chart();
        var other = Chart();
        var appointment = Visit(patient.Id, AppointmentStatus.Approved);
        var notifications = new InMemoryNotificationRepository();
        var tokens = new InMemoryDeviceTokenRepository();
        await tokens.AddAsync(new PatientDeviceToken { PatientId = patient.Id, Token = "patient-phone", Platform = "android" }, CancellationToken.None);
        await tokens.AddAsync(new PatientDeviceToken { PatientId = other.Id, Token = "other-phone", Platform = "android" }, CancellationToken.None);
        var push = new RecordingPushSender();
        var events = new PatientEventNotifier(
            notifications,
            tokens,
            push,
            new NoopUnitOfWork(),
            NullLogger<PatientEventNotifier>.Instance);
        var sut = CreateSut(patient, appointment, out _, out _, out _, events);

        var created = await sut.CreateAsync(Request(patient.Id, appointment.Id, "Ashwagandha churna"), CancellationToken.None);
        Assert.Empty(notifications.Items);

        var issued = await sut.IssueAsync(created.Id, CancellationToken.None);

        var notice = Assert.Single(notifications.Items);
        Assert.Equal(patient.Id, notice.PatientId);
        Assert.Equal(NotificationType.PrescriptionIssued, notice.Type);
        Assert.Null(notice.StaffUserId);
        Assert.Contains("Ashwagandha churna", notice.Message);
        Assert.Equal(issued.PatientId, notice.PatientId);
        var sent = Assert.Single(push.Sent);
        Assert.Equal(new[] { "patient-phone" }, sent.DeviceTokens);

        var stranger = new NotificationService(notifications, new ScriptActor(other), new NoopUnitOfWork());
        await Assert.ThrowsAsync<ForbiddenException>(() => stranger.GetForPatient(patient.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => stranger.MarkReadAsync(notice.Id, CancellationToken.None));
    }

    private static PrescriptionService CreateSut(
        Patient patient,
        Appointment appointment,
        out StubUser current,
        out List<Prescription> store,
        out Mock<IActorContext> actors,
        IPatientEventNotifier? events = null)
    {
        var doctor = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Vd. Meera Joshi",
            Email = "meera.joshi@hospital.test",
            PhoneNumber = "0770000000",
            Role = UserRole.Doctor,
            IsActive = true
        };
        current = new StubUser { UserId = doctor.Id, Email = doctor.Email, Role = UserRole.Doctor };
        var prescriptions = new FakePrescriptionRepository();
        store = prescriptions.Rows;

        var patients = new Mock<IPatientRepository>();
        patients.Setup(repo => repo.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(repo => repo.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var users = new Mock<IUserRepository>();
        users.Setup(repo => repo.GetByIdAsync(doctor.Id, It.IsAny<CancellationToken>())).ReturnsAsync(doctor);
        actors = new Mock<IActorContext>();
        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        return new PrescriptionService(
            prescriptions,
            patients.Object,
            appointments.Object,
            users.Object,
            actors.Object,
            current,
            new FakeUnitOfWork(),
            new FixedClock(),
            events ?? NullPatientEventNotifier.Instance);
    }

    private static Patient Chart() => new()
    {
        Id = Guid.NewGuid(),
        Uhid = "SAH-2026-00011",
        FirstName = "Leela",
        LastName = "Menon",
        Phone = "0771111111",
        Email = "leela@patient.test"
    };

    private static Appointment Visit(Guid patientId, AppointmentStatus status) => new()
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,
        TreatmentId = Guid.NewGuid(),
        RequestedDate = new DateOnly(2026, 10, 2),
        RequestedTimeSlot = "09:00-10:00",
        Status = status
    };

    private static CreatePrescriptionRequest Request(Guid patientId, Guid appointmentId, string name) =>
        new(patientId, appointmentId, new[] { Item(name) });

    private static PrescriptionItemRequest Item(string name) =>
        new(name, "3 g", "Twice daily after meals", "14 days", "Take with warm water");

    private sealed class StubUser : ICurrentUser
    {
        public bool IsAuthenticated { get; set; } = true;
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow { get; } = new(2026, 10, 2, 9, 30, 0, TimeSpan.Zero);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakePrescriptionRepository : IPrescriptionRepository
    {
        public List<Prescription> Rows { get; } = new();
        public List<PrescriptionRevision> Revisions { get; } = new();

        public Task<Prescription?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.FirstOrDefault(row => row.Id == id));

        public Task<(IReadOnlyList<Prescription> Items, int Total)> ListAsync(
            Guid? patientId,
            Guid? appointmentId,
            bool includeDrafts,
            bool includeSuperseded,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            IEnumerable<Prescription> query = Rows;
            if (patientId is Guid ownerId)
            {
                query = query.Where(row => row.PatientId == ownerId);
            }

            if (appointmentId is Guid visitId)
            {
                query = query.Where(row => row.AppointmentId == visitId);
            }

            if (!includeDrafts)
            {
                query = query.Where(row => row.Status != PrescriptionStatus.Draft);
            }

            if (!includeSuperseded)
            {
                query = query.Where(row => row.Status != PrescriptionStatus.Superseded);
            }

            var items = query.ToList();
            return Task.FromResult(((IReadOnlyList<Prescription>)items, items.Count));
        }

        public Task<bool> HasOpenForAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.Any(row =>
                row.AppointmentId == appointmentId
                && row.Status is PrescriptionStatus.Draft or PrescriptionStatus.Issued));

        public Task<IReadOnlyList<Prescription>> ListChainAsync(Guid rootPrescriptionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Prescription>>(
                Rows.Where(row => row.RootPrescriptionId == rootPrescriptionId)
                    .OrderBy(row => row.RevisionNumber)
                    .ToList());

        public Task<IReadOnlyList<PrescriptionRevision>> ListRevisionsAsync(
            IReadOnlyCollection<Guid> prescriptionIds,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PrescriptionRevision>>(
                Revisions.Where(row => prescriptionIds.Contains(row.RevisedPrescriptionId))
                    .OrderBy(row => row.RevisionNumber)
                    .ToList());

        public Task AddAsync(Prescription prescription, CancellationToken cancellationToken)
        {
            Rows.Add(prescription);
            return Task.CompletedTask;
        }

        public Task AddRevisionAsync(PrescriptionRevision revision, CancellationToken cancellationToken)
        {
            Revisions.Add(revision);
            return Task.CompletedTask;
        }

        public void ReplaceItems(Prescription prescription, IReadOnlyList<PrescriptionItem> items)
        {
            prescription.Items.Clear();
            prescription.Items.AddRange(items);
        }
    }
}
