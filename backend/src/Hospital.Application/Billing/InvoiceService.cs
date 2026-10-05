using System.Globalization;
using Hospital.Application.Abstractions;
using Hospital.Application.Billing.Dtos;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Billing;

public sealed class InvoiceService : IInvoiceService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IPatientRepository _patients;
    private readonly IAppointmentRepository _appointments;
    private readonly IWardRepository _wards;
    private readonly IUserRepository _users;
    private readonly IActorContext _actors;
    private readonly ICurrentUser _current;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPatientEventNotifier _events;

    public InvoiceService(
        IInvoiceRepository invoices,
        IPatientRepository patients,
        IAppointmentRepository appointments,
        IWardRepository wards,
        IUserRepository users,
        IActorContext actors,
        ICurrentUser current,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPatientEventNotifier events)
    {
        _invoices = invoices;
        _patients = patients;
        _appointments = appointments;
        _wards = wards;
        _users = users;
        _actors = actors;
        _current = current;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _events = events;
    }

    public async Task<PagedResult<InvoiceDto>> ListAsync(
        Guid? patientId,
        InvoiceStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureBillingReader();
        var (items, total) = await _invoices.ListAsync(
            patientId,
            status,
            patientVisibleOnly: false,
            page,
            pageSize,
            cancellationToken);
        return Page(items, total, page, pageSize);
    }

    public async Task<PagedResult<InvoiceDto>> ListMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (_current.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only a patient can read their own invoices.");
        }

        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var (items, total) = await _invoices.ListAsync(
            patient.Id,
            null,
            patientVisibleOnly: true,
            page,
            pageSize,
            cancellationToken);
        return Page(items, total, page, pageSize);
    }

    public async Task<InvoiceDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await RequireReadableAsync(id, cancellationToken);
        return Map(invoice);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var staff = await RequireBillingStaffAsync(cancellationToken);
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException("Patient", request.PatientId);

        var invoice = new Invoice
        {
            InvoiceNumber = NewNumber(_clock.UtcNow),
            PatientId = patient.Id,
            Currency = NormalizeCurrency(request.Currency),
            Status = InvoiceStatus.Draft,
            CreatedByUserId = staff.Id,
            Notes = NormalizeNotes(request.Notes)
        };
        invoice.Lines = await BuildLinesAsync(invoice.Id, patient.Id, request.Lines, exceptInvoiceId: null, cancellationToken);
        invoice.Total = Sum(invoice.Lines);
        await _invoices.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(invoice);
    }

    public async Task<InvoiceDto> UpdateDraftAsync(
        Guid id,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        await RequireBillingStaffAsync(cancellationToken);
        var invoice = await _invoices.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Invoice", id);
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ConflictException("Only a draft invoice can be edited.");
        }

        var lines = await BuildLinesAsync(invoice.Id, invoice.PatientId, request.Lines, invoice.Id, cancellationToken);
        var currency = NormalizeCurrency(request.Currency);
        var notes = NormalizeNotes(request.Notes);
        var total = Sum(lines);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            foreach (var line in invoice.Lines)
            {
                line.OpenSourceKey = null;
            }

            await _unitOfWork.SaveChangesAsync(ct);
            _invoices.ReplaceLines(invoice, lines);
            invoice.Currency = currency;
            invoice.Notes = notes;
            invoice.Total = total;
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        return Map(invoice);
    }

    public async Task<InvoiceDto> IssueAsync(Guid id, CancellationToken cancellationToken)
    {
        await RequireBillingStaffAsync(cancellationToken);
        var invoice = await _invoices.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Invoice", id);
        if (invoice.Status != InvoiceStatus.Draft)
        {
            throw new ConflictException("Only a draft invoice can be issued.");
        }

        if (invoice.Lines.Count == 0)
        {
            throw new DomainException("Add a treatment visit or an admission before issuing the invoice.");
        }

        await EnsureSourcesStillBillableAsync(invoice, cancellationToken);
        invoice.Status = InvoiceStatus.Issued;
        invoice.IssuedAt = _clock.UtcNow;
        await _events.PublishAsync(IssuedNotice(invoice), cancellationToken);
        return Map(invoice);
    }

    private static PatientNotice IssuedNotice(Invoice invoice)
    {
        var total = invoice.Total.ToString("0.00", CultureInfo.InvariantCulture);
        return new PatientNotice(
            invoice.PatientId,
            NotificationType.InvoiceIssued,
            "Invoice issued",
            $"Invoice {invoice.InvoiceNumber} for {invoice.Currency} {total} is ready.");
    }

    public async Task<InvoiceDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        await RequireBillingStaffAsync(cancellationToken);
        var invoice = await _invoices.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Invoice", id);
        if (invoice.Status is not (InvoiceStatus.Draft or InvoiceStatus.Issued))
        {
            throw new ConflictException("This invoice can no longer be cancelled.");
        }

        if (invoice.AmountPaid > 0)
        {
            throw new ConflictException("A payment has already been recorded, so this invoice cannot be cancelled.");
        }

        foreach (var line in invoice.Lines)
        {
            line.OpenSourceKey = null;
        }

        invoice.Status = InvoiceStatus.Cancelled;
        invoice.CancelledAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(invoice);
    }

    public async Task<InvoiceDto> RecordPaymentAsync(
        Guid id,
        RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var staff = await RequireBillingStaffAsync(cancellationToken);
        var invoice = await _invoices.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Invoice", id);

        if (invoice.Status == InvoiceStatus.Draft)
        {
            throw new ConflictException("Issue the invoice before recording a payment.");
        }

        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            throw new ConflictException("A cancelled invoice cannot take a payment.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            throw new ConflictException("This invoice is already paid.");
        }

        var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0 || !InvoiceLine.HasAtMostTwoDecimals(request.Amount))
        {
            throw new DomainException("Enter a payment amount greater than zero.");
        }

        if (request.PaidOn.Year is < DateSanity.EarliestYear or > DateSanity.LatestYear
            || request.PaidOn > _clock.UtcNow.AddDays(1))
        {
            throw new DomainException("The payment date cannot be in the future.");
        }

        var balance = invoice.Total - invoice.AmountPaid;
        if (amount > balance)
        {
            throw new DomainException("The payment is more than the balance due.");
        }

        var payment = new InvoicePayment
        {
            InvoiceId = invoice.Id,
            Amount = amount,
            Method = ParseMethod(request.Method),
            PaidOn = request.PaidOn,
            Reference = NormalizeReference(request.Reference),
            RecordedByUserId = staff.Id
        };
        await _invoices.AddPaymentAsync(payment, cancellationToken);
        if (!invoice.Payments.Contains(payment))
        {
            invoice.Payments.Add(payment);
        }

        invoice.AmountPaid = decimal.Round(invoice.AmountPaid + amount, 2, MidpointRounding.AwayFromZero);
        if (invoice.AmountPaid == invoice.Total)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = _clock.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(invoice);
    }

    private async Task<List<InvoiceLine>> BuildLinesAsync(
        Guid invoiceId,
        Guid patientId,
        IReadOnlyList<InvoiceLineRequest> requests,
        Guid? exceptInvoiceId,
        CancellationToken cancellationToken)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new DomainException("Add a treatment visit or an admission.");
        }

        if (requests.Count > InvoiceLine.MaxLines)
        {
            throw new DomainException($"An invoice can include at most {InvoiceLine.MaxLines} lines.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<InvoiceLine>(requests.Count);
        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var line = request.AppointmentId is Guid appointmentId
                ? await BuildTreatmentLineAsync(patientId, appointmentId, request, cancellationToken)
                : await BuildAdmissionLineAsync(patientId, request, cancellationToken);

            if (string.IsNullOrWhiteSpace(line.OpenSourceKey) || !seen.Add(line.OpenSourceKey))
            {
                throw new DomainException("Each treatment visit or admission can appear only once on an invoice.");
            }

            if (await _invoices.IsSourceOpenAsync(line.OpenSourceKey, exceptInvoiceId, cancellationToken))
            {
                throw new ConflictException("This treatment visit or admission is already on an open invoice.");
            }

            line.InvoiceId = invoiceId;
            line.SortOrder = index;
            lines.Add(line);
        }

        return lines;
    }

    private async Task<InvoiceLine> BuildTreatmentLineAsync(
        Guid patientId,
        Guid appointmentId,
        InvoiceLineRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AdmissionId is not null)
        {
            throw new DomainException("Each line must be one treatment visit or one admission.");
        }

        if (request.Quantity != 1)
        {
            throw new DomainException("A treatment visit is billed once.");
        }

        var appointment = await _appointments.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment", appointmentId);
        if (appointment.PatientId != patientId)
        {
            throw new DomainException("The treatment visit does not belong to this patient.");
        }

        if (appointment.Status is not (AppointmentStatus.Approved or AppointmentStatus.Completed))
        {
            throw new DomainException("Only an approved or completed treatment visit can be invoiced.");
        }

        var treatment = appointment.Treatment
            ?? throw new DomainException("The treatment visit has no treatment to charge.");
        var unitPrice = decimal.Round(treatment.UnitPrice, 2, MidpointRounding.AwayFromZero);
        if (unitPrice <= 0)
        {
            throw new DomainException("This treatment has no charge to invoice.");
        }
        return new InvoiceLine
        {
            Source = InvoiceLineSource.Treatment,
            TreatmentId = appointment.TreatmentId,
            AppointmentId = appointment.Id,
            Description = TrimDescription(treatment.Name),
            Quantity = 1,
            UnitPrice = unitPrice,
            LineTotal = InvoiceLine.CalculateLineTotal(1, unitPrice),
            OpenSourceKey = InvoiceLine.TreatmentKey(appointment.Id)
        };
    }

    private async Task<InvoiceLine> BuildAdmissionLineAsync(
        Guid patientId,
        InvoiceLineRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AppointmentId is not null || request.AdmissionId is not Guid admissionId)
        {
            throw new DomainException("Each line must be one treatment visit or one admission.");
        }

        if (request.Quantity is < 1 or > InvoiceLine.MaxQuantity)
        {
            throw new DomainException($"An admission is billed in days, from 1 to {InvoiceLine.MaxQuantity}.");
        }

        if (request.UnitPrice is not decimal dailyRate
            || dailyRate <= 0
            || dailyRate > InvoiceLine.MaxUnitPrice
            || !InvoiceLine.HasAtMostTwoDecimals(dailyRate))
        {
            throw new DomainException("An admission needs a daily rate with at most two decimal places.");
        }

        var admission = await _wards.GetAdmissionRequestByIdAsync(admissionId, cancellationToken)
            ?? throw new NotFoundException("Admission", admissionId);
        if (admission.PatientId != patientId)
        {
            throw new DomainException("The admission does not belong to this patient.");
        }

        if (admission.Status != AdmissionRequestStatus.Approved)
        {
            throw new DomainException("Only an approved admission can be invoiced.");
        }

        var unitPrice = decimal.Round(dailyRate, 2, MidpointRounding.AwayFromZero);
        var lineTotal = InvoiceLine.CalculateLineTotal(request.Quantity, unitPrice);
        if (lineTotal > InvoiceLine.MaxTotal)
        {
            throw new DomainException("The admission charge is too large.");
        }

        return new InvoiceLine
        {
            Source = InvoiceLineSource.Admission,
            AdmissionRequestId = admission.Id,
            Description = TrimDescription(AdmissionDescription(admission)),
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            LineTotal = lineTotal,
            OpenSourceKey = InvoiceLine.AdmissionKey(admission.Id)
        };
    }

    private async Task EnsureSourcesStillBillableAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        foreach (var line in invoice.Lines)
        {
            if (line.AppointmentId is Guid appointmentId)
            {
                var appointment = await _appointments.GetByIdAsync(appointmentId, cancellationToken)
                    ?? throw new DomainException("A treatment visit on this invoice no longer exists.");
                if (appointment.PatientId != invoice.PatientId
                    || appointment.Status is not (AppointmentStatus.Approved or AppointmentStatus.Completed))
                {
                    throw new DomainException("A treatment visit on this invoice is no longer approved or completed.");
                }
            }
            else if (line.AdmissionRequestId is Guid admissionId)
            {
                var admission = await _wards.GetAdmissionRequestByIdAsync(admissionId, cancellationToken)
                    ?? throw new DomainException("An admission on this invoice no longer exists.");
                if (admission.PatientId != invoice.PatientId || admission.Status != AdmissionRequestStatus.Approved)
                {
                    throw new DomainException("An admission on this invoice is no longer approved.");
                }
            }
        }
    }

    private async Task<Invoice> RequireReadableAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        var invoice = await _invoices.GetAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException("Invoice", id);

        if (_current.Role == UserRole.Patient)
        {
            var patient = await _actors.RequirePatientAsync(cancellationToken);
            if (patient.Id != invoice.PatientId)
            {
                throw new ForbiddenException("You can only read your own invoices.");
            }

            if (invoice.Status is not (InvoiceStatus.Issued or InvoiceStatus.Paid))
            {
                throw new NotFoundException("Invoice", id);
            }

            return invoice;
        }

        if (!IsBillingReader(_current.Role))
        {
            throw new ForbiddenException("Your role cannot read invoices.");
        }

        return invoice;
    }

    private void EnsureBillingReader()
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        if (!IsBillingReader(_current.Role))
        {
            throw new ForbiddenException("Your role cannot read invoices.");
        }
    }

    private async Task<User> RequireBillingStaffAsync(CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        if (_current.Role is not (UserRole.FrontDeskStaff or UserRole.Admin))
        {
            throw new ForbiddenException("Only front desk staff or an admin can bill a patient or record a payment.");
        }

        var staff = await _users.GetByIdAsync(_current.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Account was not found.");
        if (!staff.IsActive || staff.Role is not (UserRole.FrontDeskStaff or UserRole.Admin))
        {
            throw new ForbiddenException("Only front desk staff or an admin can bill a patient or record a payment.");
        }

        return staff;
    }

    private static bool IsBillingReader(UserRole role) =>
        role is UserRole.FrontDeskStaff or UserRole.Admin or UserRole.Doctor;

    private static string NewNumber(DateTimeOffset now)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"INV-{now:yyyyMMdd}-{suffix}";
    }

    private static string NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return Invoice.DefaultCurrency;
        }

        var code = currency.Trim().ToUpperInvariant();
        if (code.Length != Invoice.CurrencyLength || code.Any(ch => ch is < 'A' or > 'Z'))
        {
            throw new DomainException("Currency must be a three-letter code.");
        }

        return code;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > Invoice.NotesMaxLength)
        {
            throw new DomainException($"Notes must be at most {Invoice.NotesMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return null;
        }

        var trimmed = reference.Trim();
        if (trimmed.Length > InvoicePayment.ReferenceMaxLength)
        {
            throw new DomainException($"The payment reference must be at most {InvoicePayment.ReferenceMaxLength} characters.");
        }

        return trimmed;
    }

    private static InvoicePaymentMethod ParseMethod(string method)
    {
        if (string.IsNullOrWhiteSpace(method)
            || int.TryParse(method, out _)
            || !Enum.TryParse<InvoicePaymentMethod>(method.Trim(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw new DomainException("Payment method must be Cash, Card, or BankTransfer.");
        }

        return parsed;
    }

    private static decimal Sum(IReadOnlyList<InvoiceLine> lines)
    {
        var total = lines.Sum(line => line.LineTotal);
        if (total <= 0 || total > InvoiceLine.MaxTotal)
        {
            throw new DomainException("The invoice total must be greater than zero.");
        }

        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }

    private static string AdmissionDescription(AdmissionRequest admission)
    {
        var ward = string.IsNullOrWhiteSpace(admission.Ward?.Name) ? "Ward" : admission.Ward.Name.Trim();
        var reason = admission.Reason?.Trim();
        return string.IsNullOrWhiteSpace(reason) ? $"Ward stay — {ward}" : $"Ward stay — {ward}: {reason}";
    }

    private static string TrimDescription(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Charge" : value.Trim();
        return text.Length <= InvoiceLine.DescriptionMaxLength
            ? text
            : text[..InvoiceLine.DescriptionMaxLength];
    }

    private static PagedResult<InvoiceDto> Page(
        IReadOnlyList<Invoice> items,
        int total,
        int page,
        int pageSize) =>
        new()
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

    private static InvoiceDto Map(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.PatientId,
            invoice.Currency,
            invoice.Status,
            invoice.Total,
            invoice.AmountPaid,
            invoice.Total - invoice.AmountPaid,
            invoice.CreatedByUserId,
            invoice.CreatedAt,
            invoice.UpdatedAt,
            invoice.IssuedAt,
            invoice.PaidAt,
            invoice.CancelledAt,
            invoice.Notes,
            invoice.Lines
                .OrderBy(line => line.SortOrder)
                .ThenBy(line => line.CreatedAt)
                .Select(line => new InvoiceLineDto(
                    line.Id,
                    line.Source,
                    line.AppointmentId,
                    line.TreatmentId,
                    line.AdmissionRequestId,
                    line.Description,
                    line.Quantity,
                    line.UnitPrice,
                    line.LineTotal))
                .ToList(),
            invoice.Payments
                .OrderBy(payment => payment.PaidOn)
                .ThenBy(payment => payment.CreatedAt)
                .Select(payment => new InvoicePaymentDto(
                    payment.Id,
                    payment.Amount,
                    payment.Method,
                    payment.PaidOn,
                    payment.Reference,
                    payment.RecordedByUserId))
                .ToList());
}
