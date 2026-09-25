using Hospital.Application.Abstractions;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Appointments;

public sealed class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointments;
    private readonly IPatientRepository _patients;
    private readonly ITreatmentRepository _treatments;
    private readonly IBookingValidator _bookingValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AppointmentService(
        IAppointmentRepository appointments,
        IPatientRepository patients,
        ITreatmentRepository treatments,
        IBookingValidator bookingValidator,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _appointments = appointments;
        _patients = patients;
        _treatments = treatments;
        _bookingValidator = bookingValidator;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        var treatment = await _treatments.GetByIdAsync(request.TreatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), request.TreatmentId);

        TreatmentSchedule? schedule = null;
        if (request.ScheduleId is { } scheduleId)
        {
            schedule = await _treatments.GetScheduleByIdAsync(scheduleId, cancellationToken)
                ?? throw new NotFoundException(nameof(TreatmentSchedule), scheduleId);

            if (schedule.TreatmentId != treatment.Id)
            {
                throw new DomainException("Schedule does not belong to the requested treatment.");
            }

            // Validate booking date/time against the schedule
            _bookingValidator.Validate(schedule, request.RequestedDate, request.RequestedTimeSlot);
            // Check capacity based on schedule.MaxPatients
            if (schedule.MaxPatients > 0)
            {
                var capacitySlot = request.RequestedTimeSlot.Trim();
                if (await _appointments.HasActiveSlotAsync(
                        patient.Id, treatment.Id, request.RequestedDate, capacitySlot, excludeId: null, cancellationToken))
                {
                    throw new ConflictException("This patient already has an appointment for that treatment date and time slot.");
                }

                var capacityAppointment = CreateAppointment(patient, treatment, schedule, request, capacitySlot);
                if (!await _appointments.TryAddWithinCapacityAsync(capacityAppointment, schedule.MaxPatients, cancellationToken))
                {
                    throw new ConflictException("Selected time slot is full.");
                }

                return Map(capacityAppointment);
            }
        }

        var slot = request.RequestedTimeSlot.Trim();
        if (await _appointments.HasActiveSlotAsync(
                patient.Id, treatment.Id, request.RequestedDate, slot, excludeId: null, cancellationToken))
        {
            throw new ConflictException("This patient already has an appointment for that treatment date and time slot.");
        }

        var appointment = CreateAppointment(patient, treatment, schedule, request, slot);

        await _appointments.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(appointment);
    }

    private static Appointment CreateAppointment(
        Patient patient,
        Treatment treatment,
        TreatmentSchedule? schedule,
        CreateAppointmentRequest request,
        string slot) => new()
        {
            PatientId = patient.Id,
            Patient = patient,
            TreatmentId = treatment.Id,
            Treatment = treatment,
            ScheduleId = schedule?.Id,
            Schedule = schedule,
            RequestedDate = request.RequestedDate,
            RequestedTimeSlot = slot,
            Status = AppointmentStatus.Pending
        };

    public async Task CancelAsync(Guid appointmentId, Guid requestingPatientId, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), appointmentId);

        if (appointment.PatientId != requestingPatientId)
        {
            throw new UnauthorizedException("Only the owning patient can cancel their appointment.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AppointmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), id);
        return Map(appointment);
    }

    public async Task<PagedResult<AppointmentDto>> ListAsync(
        DateOnly? onDate,
        Guid? patientId,
        Guid? treatmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _appointments.ListAsync(onDate, patientId, treatmentId, page, pageSize, cancellationToken);
        return new PagedResult<AppointmentDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AppointmentDto> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken)
    {
        var appointment = await _appointments.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Appointment), id);

        appointment.Status = request.Status;
        if (request.Status is AppointmentStatus.Approved or AppointmentStatus.Rejected)
        {
            appointment.DecidedById = request.DecidedBy;
            appointment.DecidedAt = _clock.UtcNow;
        }

        appointment.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(appointment);
    }

    private static AppointmentDto Map(Appointment a) => new(
        a.Id,
        a.PatientId,
        $"{a.Patient.FirstName} {a.Patient.LastName}",
        a.Patient.Uhid,
        a.TreatmentId,
        a.Treatment.Name,
        a.ScheduleId,
        a.RequestedDate,
        a.RequestedTimeSlot,
        a.Status,
        a.DecidedById,
        a.DecidedAt);
}
