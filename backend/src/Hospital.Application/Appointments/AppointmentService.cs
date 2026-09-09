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
    private readonly IStaffUserRepository _staffUsers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AppointmentService(
        IAppointmentRepository appointments,
        IPatientRepository patients,
        IStaffUserRepository staffUsers,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _appointments = appointments;
        _patients = patients;
        _staffUsers = staffUsers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), request.PatientId);

        var doctor = await _staffUsers.GetByIdAsync(request.DoctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor", request.DoctorId);

        if (doctor.Role is not StaffRole.Doctor and not StaffRole.Admin)
        {
            throw new DomainException("Appointments can only be assigned to a doctor.");
        }

        var end = request.ScheduledAt.AddMinutes(request.DurationMinutes);
        if (await _appointments.HasOverlapAsync(doctor.Id, request.ScheduledAt, end, excludeId: null, cancellationToken))
        {
            throw new ConflictException("This doctor already has an appointment in that time slot.");
        }

        var appointment = new Appointment
        {
            PatientId = patient.Id,
            Patient = patient,
            DoctorId = doctor.Id,
            Doctor = doctor,
            ScheduledAt = request.ScheduledAt,
            EndsAt = end,
            DurationMinutes = request.DurationMinutes,
            Reason = request.Reason.Trim(),
            Notes = request.Notes
        };

        await _appointments.AddAsync(appointment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(appointment);
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
        Guid? doctorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _appointments.ListAsync(onDate, patientId, doctorId, page, pageSize, cancellationToken);
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

        if (request.Status == AppointmentStatus.Cancelled)
        {
            if (string.IsNullOrWhiteSpace(request.CancellationReason))
            {
                throw new DomainException("A cancellation reason is required.");
            }

            appointment.CancellationReason = request.CancellationReason.Trim();
        }

        appointment.Status = request.Status;
        appointment.UpdatedAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(appointment);
    }

    private static AppointmentDto Map(Appointment a) => new(
        a.Id,
        a.PatientId,
        $"{a.Patient.FirstName} {a.Patient.LastName}",
        a.Patient.Uhid,
        a.DoctorId,
        a.Doctor.FullName,
        a.ScheduledAt,
        a.DurationMinutes,
        a.Status,
        a.Reason,
        a.Notes);
}
