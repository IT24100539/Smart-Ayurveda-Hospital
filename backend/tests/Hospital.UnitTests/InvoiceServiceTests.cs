using Hospital.Application.Abstractions;
using Hospital.Application.Billing;
using Hospital.Application.Billing.Dtos;
using Hospital.Application.Billing.Validators;
using Hospital.Application.Communication;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Hospital.UnitTests;

public sealed class InvoiceServiceTests
{
    private static readonly DateTimeOffset PaidOn = new(2026, 10, 2, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateValidator_RejectsALineThatIsNeitherTreatmentNorAdmission()
    {
        var validator = new CreateInvoiceRequestValidator();
        var result = validator.Validate(new CreateInvoiceRequest(
            Guid.NewGuid(),
            null,
            null,
            new[] { new InvoiceLineRequest(null, null, 1, null) }));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateValidator_RejectsATreatmentPriceOverride()
    {
        var validator = new CreateInvoiceRequestValidator();
        var result = validator.Validate(new CreateInvoiceRequest(
            Guid.NewGuid(),
            "LKR",
            null,
            new[] { new InvoiceLineRequest(Guid.NewGuid(), null, 1, 50m) }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("catalog price", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Create_SumsCatalogPriceAndAdmissionDays_AndDefaultsCurrencyToLkr()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Completed, 1800.50m, "Abhyanga");
        var stay = Stay(patient.Id, AdmissionRequestStatus.Approved);
        var sut = CreateSut(patient, visit, stay, out _, out _, out _);

        var created = await sut.CreateAsync(
            new CreateInvoiceRequest(
                patient.Id,
                "  ",
                " Panchakarma package ",
                new[]
                {
                    new InvoiceLineRequest(visit.Id, null, 1, 1m),
                    new InvoiceLineRequest(null, stay.Id, 3, 100.10m)
                }),
            CancellationToken.None);

        Assert.Equal(InvoiceStatus.Draft, created.Status);
        Assert.Equal("LKR", created.Currency);
        Assert.Equal("Panchakarma package", created.Notes);
        Assert.Equal(2100.80m, created.Total);
        Assert.Equal(2100.80m, created.Balance);
        Assert.Equal(0m, created.AmountPaid);

        var treatment = Assert.Single(created.Lines, line => line.Source == InvoiceLineSource.Treatment);
        Assert.Equal(1800.50m, treatment.UnitPrice);
        Assert.Equal(1800.50m, treatment.LineTotal);
        Assert.Equal("Abhyanga", treatment.Description);
        Assert.Equal(visit.TreatmentId, treatment.TreatmentId);

        var admission = Assert.Single(created.Lines, line => line.Source == InvoiceLineSource.Admission);
        Assert.Equal(3, admission.Quantity);
        Assert.Equal(100.10m, admission.UnitPrice);
        Assert.Equal(300.30m, admission.LineTotal);
        Assert.Contains("Female Kayachikitsa", admission.Description);
    }

    [Fact]
    public async Task Create_KeepsAnExplicitCurrency()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Approved, 500m, "Nasya");
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out _, out _, out _);

        var created = await sut.CreateAsync(Bill(patient.Id, visit.Id, currency: "usd"), CancellationToken.None);

        Assert.Equal("USD", created.Currency);
        Assert.Equal(500m, created.Total);
    }

    [Fact]
    public async Task Create_RejectsAVisitThatIsNotApprovedOrCompleted()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Pending, 500m, "Nasya");
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out _, out _, out _);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None));

        Assert.Contains("approved or completed", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_RejectsAnAdmissionThatIsNotApproved()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Approved, 500m, "Nasya");
        var stay = Stay(patient.Id, AdmissionRequestStatus.Pending);
        var sut = CreateSut(patient, visit, stay, out _, out _, out _);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            sut.CreateAsync(BillStay(patient.Id, stay.Id), CancellationToken.None));

        Assert.Contains("approved admission", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_RejectsAVisitThatBelongsToAnotherPatient()
    {
        var patient = Chart();
        var visit = Visit(Guid.NewGuid(), AppointmentStatus.Approved, 500m, "Nasya");
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out _, out _, out _);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None));

        Assert.Contains("does not belong", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StatusTransitions_IssuePaymentAndCancel()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Approved, 1000m, "Shirodhara");
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out var current, out var store, out var actors);

        var created = await sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None);
        Assert.Equal(InvoiceStatus.Draft, created.Status);

        current.Role = UserRole.Patient;
        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetAsync(created.Id, CancellationToken.None));
        var hidden = await sut.ListMineAsync(1, 20, CancellationToken.None);
        Assert.Empty(hidden.Items);

        current.Role = UserRole.FrontDeskStaff;
        var payEarly = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RecordPaymentAsync(created.Id, Payment(1000m, "Cash"), CancellationToken.None));
        Assert.Contains("Issue the invoice", payEarly.Message, StringComparison.OrdinalIgnoreCase);

        visit.Status = AppointmentStatus.Cancelled;
        var blocked = await Assert.ThrowsAsync<DomainException>(() => sut.IssueAsync(created.Id, CancellationToken.None));
        Assert.Contains("no longer approved or completed", blocked.Message, StringComparison.OrdinalIgnoreCase);

        visit.Status = AppointmentStatus.Completed;
        var issued = await sut.IssueAsync(created.Id, CancellationToken.None);
        Assert.Equal(InvoiceStatus.Issued, issued.Status);
        Assert.NotNull(issued.IssuedAt);

        var edit = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.UpdateDraftAsync(created.Id, new UpdateInvoiceRequest(null, null, new[] { Line(visit.Id) }), CancellationToken.None));
        Assert.Contains("draft", edit.Message, StringComparison.OrdinalIgnoreCase);

        var partial = await sut.RecordPaymentAsync(created.Id, Payment(400m, "Cash", "RCPT-1"), CancellationToken.None);
        Assert.Equal(InvoiceStatus.Issued, partial.Status);
        Assert.Equal(400m, partial.AmountPaid);
        Assert.Equal(600m, partial.Balance);
        Assert.Equal("RCPT-1", Assert.Single(partial.Payments).Reference);
        Assert.Equal(InvoicePaymentMethod.Cash, partial.Payments[0].Method);

        var tooMuch = await Assert.ThrowsAsync<DomainException>(() =>
            sut.RecordPaymentAsync(created.Id, Payment(600.01m, "Card"), CancellationToken.None));
        Assert.Contains("balance", tooMuch.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(400m, store.Single().AmountPaid);

        var cannotCancel = await Assert.ThrowsAsync<ConflictException>(() => sut.CancelAsync(created.Id, CancellationToken.None));
        Assert.Contains("payment has already been recorded", cannotCancel.Message, StringComparison.OrdinalIgnoreCase);

        var paid = await sut.RecordPaymentAsync(created.Id, Payment(600m, "BankTransfer", "TRX-9"), CancellationToken.None);
        Assert.Equal(InvoiceStatus.Paid, paid.Status);
        Assert.Equal(0m, paid.Balance);
        Assert.NotNull(paid.PaidAt);
        Assert.Equal(2, paid.Payments.Count);

        var again = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.RecordPaymentAsync(created.Id, Payment(1m, "Cash"), CancellationToken.None));
        Assert.Contains("already paid", again.Message, StringComparison.OrdinalIgnoreCase);

        await Assert.ThrowsAsync<ConflictException>(() => sut.CancelAsync(created.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictException>(() => sut.IssueAsync(created.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CancelledDraft_ReleasesTheVisitSoItCanBeBilledAgain()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Approved, 750m, "Nasya");
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out _, out _, out _);

        var first = await sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None);
        var duplicate = await Assert.ThrowsAsync<ConflictException>(() =>
            sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None));
        Assert.Contains("already on an open invoice", duplicate.Message, StringComparison.OrdinalIgnoreCase);

        var cancelled = await sut.CancelAsync(first.Id, CancellationToken.None);
        Assert.Equal(InvoiceStatus.Cancelled, cancelled.Status);

        var second = await sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None);
        Assert.Equal(InvoiceStatus.Draft, second.Status);
        Assert.Equal(750m, second.Total);
    }

    [Fact]
    public async Task PatientA_CannotReadPatientB_AndOnlySeesIssuedInvoices()
    {
        var patientA = Chart();
        var patientB = Chart();
        var visit = Visit(patientA.Id, AppointmentStatus.Approved, 500m, "Abhyanga");
        var sut = CreateSut(patientA, visit, Stay(patientA.Id, AdmissionRequestStatus.Approved), out var current, out _, out var actors);
        var created = await sut.CreateAsync(Bill(patientA.Id, visit.Id), CancellationToken.None);
        await sut.IssueAsync(created.Id, CancellationToken.None);

        current.Role = UserRole.Patient;
        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patientB);
        var denied = await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetAsync(created.Id, CancellationToken.None));
        Assert.Contains("your own", denied.Message, StringComparison.OrdinalIgnoreCase);

        var otherMine = await sut.ListMineAsync(1, 20, CancellationToken.None);
        Assert.Empty(otherMine.Items);

        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patientA);
        var own = await sut.GetAsync(created.Id, CancellationToken.None);
        Assert.Equal(patientA.Id, own.PatientId);
        Assert.Equal(InvoiceStatus.Issued, own.Status);

        var mine = await sut.ListMineAsync(1, 20, CancellationToken.None);
        Assert.Equal(created.Id, Assert.Single(mine.Items).Id);
    }

    [Fact]
    public async Task DoctorCannotBill_TherapistCannotRead_FrontDeskCan()
    {
        var patient = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Completed, 500m, "Abhyanga");
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out var current, out _, out _);
        var created = await sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None);
        await sut.IssueAsync(created.Id, CancellationToken.None);

        current.Role = UserRole.Doctor;
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.RecordPaymentAsync(created.Id, Payment(500m, "Cash"), CancellationToken.None));
        var readable = await sut.GetAsync(created.Id, CancellationToken.None);
        Assert.Equal(created.Id, readable.Id);

        current.Role = UserRole.Therapist;
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetAsync(created.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => sut.ListAsync(null, null, 1, 20, CancellationToken.None));

        current.Role = UserRole.FrontDeskStaff;
        var paid = await sut.RecordPaymentAsync(created.Id, Payment(500m, "Card", "POS-4"), CancellationToken.None);
        Assert.Equal(InvoiceStatus.Paid, paid.Status);
    }

    [Fact]
    public async Task Issue_StoresAnEventForThatPatientOnly()
    {
        var patient = Chart();
        var other = Chart();
        var visit = Visit(patient.Id, AppointmentStatus.Approved, 1000m, "Shirodhara");
        var notifications = new InMemoryNotificationRepository();
        var tokens = new InMemoryDeviceTokenRepository();
        await tokens.AddAsync(new PatientDeviceToken { PatientId = patient.Id, Token = "patient-phone", Platform = "web" }, CancellationToken.None);
        await tokens.AddAsync(new PatientDeviceToken { PatientId = other.Id, Token = "other-phone", Platform = "web" }, CancellationToken.None);
        var push = new RecordingPushSender();
        var events = new PatientEventNotifier(
            notifications,
            tokens,
            push,
            new NoopUnitOfWork(),
            NullLogger<PatientEventNotifier>.Instance);
        var sut = CreateSut(patient, visit, Stay(patient.Id, AdmissionRequestStatus.Approved), out _, out _, out _, events);

        var created = await sut.CreateAsync(Bill(patient.Id, visit.Id), CancellationToken.None);
        Assert.Empty(notifications.Items);

        var issued = await sut.IssueAsync(created.Id, CancellationToken.None);

        var notice = Assert.Single(notifications.Items);
        Assert.Equal(patient.Id, notice.PatientId);
        Assert.Equal(NotificationType.InvoiceIssued, notice.Type);
        Assert.Null(notice.StaffUserId);
        Assert.Contains(issued.InvoiceNumber, notice.Message);
        Assert.Contains("LKR 1000.00", notice.Message);
        var sent = Assert.Single(push.Sent);
        Assert.Equal(new[] { "patient-phone" }, sent.DeviceTokens);

        var stranger = new NotificationService(notifications, new ScriptActor(other), new NoopUnitOfWork());
        await Assert.ThrowsAsync<ForbiddenException>(() => stranger.GetForPatient(patient.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ForbiddenException>(() => stranger.MarkReadAsync(notice.Id, CancellationToken.None));
    }

    private static InvoiceService CreateSut(
        Patient patient,
        Appointment appointment,
        AdmissionRequest admission,
        out StubUser current,
        out List<Invoice> store,
        out Mock<IActorContext> actors,
        IPatientEventNotifier? events = null)
    {
        var staff = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nimali Perera",
            Email = "nimali.perera@hospital.test",
            PhoneNumber = "0770000000",
            Role = UserRole.FrontDeskStaff,
            IsActive = true
        };
        current = new StubUser { UserId = staff.Id, Email = staff.Email, Role = UserRole.FrontDeskStaff };
        var invoices = new FakeInvoiceRepository();
        store = invoices.Rows;

        var patients = new Mock<IPatientRepository>();
        patients.Setup(repo => repo.GetByIdAsync(patient.Id, It.IsAny<CancellationToken>())).ReturnsAsync(patient);
        var appointments = new Mock<IAppointmentRepository>();
        appointments.Setup(repo => repo.GetByIdAsync(appointment.Id, It.IsAny<CancellationToken>())).ReturnsAsync(appointment);
        var wards = new Mock<IWardRepository>();
        wards.Setup(repo => repo.GetAdmissionRequestByIdAsync(admission.Id, It.IsAny<CancellationToken>())).ReturnsAsync(admission);
        var users = new Mock<IUserRepository>();
        users.Setup(repo => repo.GetByIdAsync(staff.Id, It.IsAny<CancellationToken>())).ReturnsAsync(staff);
        actors = new Mock<IActorContext>();
        actors.Setup(actor => actor.RequirePatientAsync(It.IsAny<CancellationToken>())).ReturnsAsync(patient);

        return new InvoiceService(
            invoices,
            patients.Object,
            appointments.Object,
            wards.Object,
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
        Uhid = "SAH-2026-00021",
        FirstName = "Leela",
        LastName = "Menon",
        Phone = "0771111111",
        Email = "leela@patient.test"
    };

    private static Appointment Visit(Guid patientId, AppointmentStatus status, decimal unitPrice, string name)
    {
        var treatment = new Treatment
        {
            Id = Guid.NewGuid(),
            Name = name,
            UnitPrice = unitPrice,
            IsActive = true
        };
        return new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            TreatmentId = treatment.Id,
            Treatment = treatment,
            RequestedDate = new DateOnly(2026, 10, 2),
            RequestedTimeSlot = "09:00-10:00",
            Status = status
        };
    }

    private static AdmissionRequest Stay(Guid patientId, AdmissionRequestStatus status) => new()
    {
        Id = Guid.NewGuid(),
        PatientId = patientId,
        Status = status,
        Reason = "Virechana recovery",
        PreferredDate = new DateOnly(2026, 10, 3),
        Ward = new Ward { Id = Guid.NewGuid(), Name = "Female Kayachikitsa", Gender = WardGender.Female, TotalCapacity = 6 }
    };

    private static CreateInvoiceRequest Bill(Guid patientId, Guid appointmentId, string? currency = null) =>
        new(patientId, currency, null, new[] { Line(appointmentId) });

    private static CreateInvoiceRequest BillStay(Guid patientId, Guid admissionId) =>
        new(patientId, null, null, new[] { new InvoiceLineRequest(null, admissionId, 1, 2500m) });

    private static InvoiceLineRequest Line(Guid appointmentId) => new(appointmentId, null, 1, null);

    private static RecordPaymentRequest Payment(decimal amount, string method, string? reference = null) =>
        new(amount, method, PaidOn, reference);

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

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public List<Invoice> Rows { get; } = new();

        public Task<Invoice?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.FirstOrDefault(row => row.Id == id));

        public Task<(IReadOnlyList<Invoice> Items, int Total)> ListAsync(
            Guid? patientId,
            InvoiceStatus? status,
            bool patientVisibleOnly,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            IEnumerable<Invoice> query = Rows;
            if (patientId is Guid ownerId)
            {
                query = query.Where(row => row.PatientId == ownerId);
            }

            if (patientVisibleOnly)
            {
                query = query.Where(row => row.Status is InvoiceStatus.Issued or InvoiceStatus.Paid);
            }
            else if (status is InvoiceStatus filtered)
            {
                query = query.Where(row => row.Status == filtered);
            }

            var items = query.ToList();
            return Task.FromResult(((IReadOnlyList<Invoice>)items, items.Count));
        }

        public Task<bool> IsSourceOpenAsync(string openSourceKey, Guid? exceptInvoiceId, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.Any(invoice =>
                (exceptInvoiceId is null || invoice.Id != exceptInvoiceId)
                && invoice.Lines.Any(line => line.OpenSourceKey == openSourceKey)));

        public Task AddAsync(Invoice invoice, CancellationToken cancellationToken)
        {
            Rows.Add(invoice);
            return Task.CompletedTask;
        }

        public Task AddPaymentAsync(InvoicePayment payment, CancellationToken cancellationToken)
        {
            var invoice = Rows.FirstOrDefault(row => row.Id == payment.InvoiceId);
            if (invoice is not null && !invoice.Payments.Contains(payment))
            {
                invoice.Payments.Add(payment);
            }

            return Task.CompletedTask;
        }

        public void ReplaceLines(Invoice invoice, IReadOnlyList<InvoiceLine> lines)
        {
            invoice.Lines.Clear();
            invoice.Lines.AddRange(lines);
        }
    }
}
