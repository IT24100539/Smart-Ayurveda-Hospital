using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Application.Prescriptions.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Prescriptions;

public sealed class PrescriptionService : IPrescriptionService
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IPatientRepository _patients;
    private readonly IAppointmentRepository _appointments;
    private readonly IUserRepository _users;
    private readonly IActorContext _actors;
    private readonly ICurrentUser _current;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPatientEventNotifier _events;

    public PrescriptionService(
        IPrescriptionRepository prescriptions,
        IPatientRepository patients,
        IAppointmentRepository appointments,
        IUserRepository users,
        IActorContext actors,
        ICurrentUser current,
        IUnitOfWork unitOfWork,
        IClock clock,
        IPatientEventNotifier events)
    {
        _prescriptions = prescriptions;
        _patients = patients;
        _appointments = appointments;
        _users = users;
        _actors = actors;
        _current = current;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _events = events;
    }

    public async Task<PagedResult<PrescriptionDto>> ListAsync(
        Guid? patientId,
        Guid? appointmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureClinicalReader();
        var (items, total) = await _prescriptions.ListAsync(
            patientId,
            appointmentId,
            includeDrafts: true,
            includeSuperseded: false,
            page,
            pageSize,
            cancellationToken);
        return Page(items, total, page, pageSize);
    }

    public async Task<PagedResult<PrescriptionDto>> ListMineAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (_current.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only a patient can read their own prescriptions.");
        }

        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var (items, total) = await _prescriptions.ListAsync(
            patient.Id,
            null,
            includeDrafts: false,
            includeSuperseded: false,
            page,
            pageSize,
            cancellationToken);
        return Page(items, total, page, pageSize);
    }

    public async Task<PrescriptionDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await RequireReadableAsync(id, cancellationToken);
        return Map(prescription);
    }

    public async Task<PrescriptionHistoryDto> GetHistoryAsync(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await RequireReadableAsync(id, cancellationToken);
        var chain = await _prescriptions.ListChainAsync(prescription.RootPrescriptionId, cancellationToken);
        if (_current.Role == UserRole.Patient)
        {
            chain = chain.Where(item => item.Status != PrescriptionStatus.Draft).ToList();
        }

        var revisions = await _prescriptions.ListRevisionsAsync(chain.Select(item => item.Id).ToArray(), cancellationToken);
        return new PrescriptionHistoryDto(
            prescription.RootPrescriptionId,
            chain.Select(Map).ToList(),
            revisions.Select(MapRevision).ToList());
    }

    public async Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var doctor = await RequireDoctorAsync(cancellationToken);
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException("Patient", request.PatientId);
        var appointment = await _appointments.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment", request.AppointmentId);
        if (appointment.PatientId != patient.Id)
        {
            throw new DomainException("The appointment does not belong to this patient.");
        }

        if (appointment.Status is not (AppointmentStatus.Approved or AppointmentStatus.Completed))
        {
            throw new DomainException("A prescription can only be written for an approved or completed visit.");
        }

        if (await _prescriptions.HasOpenForAppointmentAsync(appointment.Id, cancellationToken))
        {
            throw new ConflictException("This visit already has an open prescription.");
        }

        var prescription = new Prescription
        {
            PatientId = patient.Id,
            AppointmentId = appointment.Id,
            DoctorUserId = doctor.Id,
            DoctorName = DoctorName(doctor),
            Status = PrescriptionStatus.Draft,
            RevisionNumber = 1
        };
        prescription.RootPrescriptionId = prescription.Id;
        prescription.Items = MapItems(prescription.Id, request.Items);
        await _prescriptions.AddAsync(prescription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(prescription);
    }

    public async Task<PrescriptionDto> UpdateDraftAsync(
        Guid id,
        UpdatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        await RequireDoctorAsync(cancellationToken);
        var prescription = await _prescriptions.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Prescription", id);
        if (prescription.Status != PrescriptionStatus.Draft)
        {
            throw new ConflictException(
                "This prescription can no longer be edited. After it is issued, changes are recorded as a revision.");
        }

        _prescriptions.ReplaceItems(prescription, MapItems(prescription.Id, request.Items));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(prescription);
    }

    public async Task<PrescriptionDto> IssueAsync(Guid id, CancellationToken cancellationToken)
    {
        await RequireDoctorAsync(cancellationToken);
        var prescription = await _prescriptions.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Prescription", id);
        if (prescription.Status != PrescriptionStatus.Draft)
        {
            throw new ConflictException("Only a draft prescription can be issued.");
        }

        if (prescription.Items.Count == 0)
        {
            throw new DomainException("Add at least one medicine before issuing the prescription.");
        }

        var now = _clock.UtcNow;
        prescription.Status = PrescriptionStatus.Issued;
        prescription.IssuedAt = now;
        await _events.PublishAsync(IssuedNotice(prescription), cancellationToken);
        return Map(prescription);
    }

    public async Task<PrescriptionDto> ReviseAsync(
        Guid id,
        RevisePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var doctor = await RequireDoctorAsync(cancellationToken);
        Prescription? created = null;
        PatientNotice? issued = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var previous = await _prescriptions.GetAsync(id, tracking: true, ct)
                ?? throw new NotFoundException("Prescription", id);
            if (previous.Status != PrescriptionStatus.Issued)
            {
                throw new ConflictException("Only an issued prescription can be revised.");
            }

            var now = _clock.UtcNow;
            previous.Status = PrescriptionStatus.Superseded;
            previous.SupersededAt = now;
            await _unitOfWork.SaveChangesAsync(ct);

            var next = new Prescription
            {
                PatientId = previous.PatientId,
                AppointmentId = previous.AppointmentId,
                DoctorUserId = doctor.Id,
                DoctorName = DoctorName(doctor),
                Status = PrescriptionStatus.Issued,
                RevisionNumber = previous.RevisionNumber + 1,
                RootPrescriptionId = previous.RootPrescriptionId == Guid.Empty
                    ? previous.Id
                    : previous.RootPrescriptionId,
                RevisesPrescriptionId = previous.Id,
                IssuedAt = now
            };
            next.Items = MapItems(next.Id, request.Items);
            await _prescriptions.AddAsync(next, ct);
            previous.SupersededByPrescriptionId = next.Id;
            await _prescriptions.AddRevisionAsync(new PrescriptionRevision
            {
                PreviousPrescriptionId = previous.Id,
                RevisedPrescriptionId = next.Id,
                RevisionNumber = next.RevisionNumber,
                RevisedByUserId = doctor.Id,
                RevisedAt = now,
                Reason = request.Reason.Trim()
            }, ct);
            issued = IssuedNotice(next);
            await _events.StageAsync(issued, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            created = next;
        }, cancellationToken);

        if (created is null || issued is null)
        {
            throw new DomainException("The revision was not recorded.");
        }

        await _events.DeliverAsync(issued, cancellationToken);
        return Map(created);
    }

    private static PatientNotice IssuedNotice(Prescription prescription)
    {
        var doctor = string.IsNullOrWhiteSpace(prescription.DoctorName) ? "Your vaidya" : prescription.DoctorName.Trim();
        var medicine = prescription.Items.FirstOrDefault()?.Name?.Trim();
        var message = string.IsNullOrWhiteSpace(medicine)
            ? $"{doctor} issued a prescription."
            : $"{doctor} issued a prescription that includes {medicine}.";
        return new PatientNotice(
            prescription.PatientId,
            NotificationType.PrescriptionIssued,
            "Prescription issued",
            message);
    }

    public async Task<PrescriptionDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        await RequireDoctorAsync(cancellationToken);
        var prescription = await _prescriptions.GetAsync(id, tracking: true, cancellationToken)
            ?? throw new NotFoundException("Prescription", id);
        if (prescription.Status is not (PrescriptionStatus.Draft or PrescriptionStatus.Issued))
        {
            throw new ConflictException("This prescription can no longer be cancelled.");
        }

        prescription.Status = PrescriptionStatus.Cancelled;
        prescription.CancelledAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(prescription);
    }

    private async Task<Prescription> RequireReadableAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        if (_current.Role == UserRole.Patient)
        {
            var prescription = await _prescriptions.GetAsync(id, tracking: false, cancellationToken)
                ?? throw new NotFoundException("Prescription", id);
            var patient = await _actors.RequirePatientAsync(cancellationToken);
            if (patient.Id != prescription.PatientId)
            {
                throw new ForbiddenException("You can only read your own prescriptions.");
            }

            if (prescription.Status == PrescriptionStatus.Draft)
            {
                throw new NotFoundException("Prescription", id);
            }

            return prescription;
        }

        if (!IsClinicalReader(_current.Role))
        {
            throw new ForbiddenException("Your role cannot read prescriptions.");
        }

        return await _prescriptions.GetAsync(id, tracking: false, cancellationToken)
            ?? throw new NotFoundException("Prescription", id);
    }

    private void EnsureClinicalReader()
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        if (!IsClinicalReader(_current.Role))
        {
            throw new ForbiddenException("Your role cannot read prescriptions.");
        }
    }

    private static bool IsClinicalReader(UserRole role) =>
        role is UserRole.Doctor or UserRole.Admin or UserRole.Therapist;

    private async Task<User> RequireDoctorAsync(CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        if (_current.Role != UserRole.Doctor)
        {
            throw new ForbiddenException("Only a doctor can write a prescription.");
        }

        var doctor = await _users.GetByIdAsync(_current.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Account was not found.");
        if (!doctor.IsActive || doctor.Role != UserRole.Doctor)
        {
            throw new ForbiddenException("Only a doctor can write a prescription.");
        }

        return doctor;
    }

    private static string DoctorName(User doctor)
    {
        var name = string.IsNullOrWhiteSpace(doctor.FullName) ? doctor.Email : doctor.FullName.Trim();
        return name.Length <= Prescription.DoctorNameMaxLength
            ? name
            : name[..Prescription.DoctorNameMaxLength];
    }

    private static List<PrescriptionItem> MapItems(Guid prescriptionId, IReadOnlyList<PrescriptionItemRequest> items)
    {
        var mapped = new List<PrescriptionItem>(items.Count);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            mapped.Add(new PrescriptionItem
            {
                PrescriptionId = prescriptionId,
                SortOrder = index,
                Name = item.Name.Trim(),
                Dosage = item.Dosage.Trim(),
                Frequency = item.Frequency.Trim(),
                Duration = item.Duration.Trim(),
                Instructions = item.Instructions?.Trim() ?? string.Empty
            });
        }

        return mapped;
    }

    private static PagedResult<PrescriptionDto> Page(
        IReadOnlyList<Prescription> items,
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

    private static PrescriptionDto Map(Prescription prescription) =>
        new(
            prescription.Id,
            prescription.PatientId,
            prescription.AppointmentId,
            prescription.DoctorUserId,
            prescription.DoctorName,
            prescription.Status,
            prescription.RevisionNumber,
            prescription.RootPrescriptionId,
            prescription.RevisesPrescriptionId,
            prescription.SupersededByPrescriptionId,
            prescription.CreatedAt,
            prescription.UpdatedAt,
            prescription.IssuedAt,
            prescription.CancelledAt,
            prescription.SupersededAt,
            prescription.Items
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.CreatedAt)
                .Select(item => new PrescriptionItemDto(
                    item.Id,
                    item.Name,
                    item.Dosage,
                    item.Frequency,
                    item.Duration,
                    item.Instructions))
                .ToList());

    private static PrescriptionRevisionDto MapRevision(PrescriptionRevision revision) =>
        new(
            revision.Id,
            revision.PreviousPrescriptionId,
            revision.RevisedPrescriptionId,
            revision.RevisionNumber,
            revision.RevisedByUserId,
            revision.Reason,
            revision.RevisedAt);
}
