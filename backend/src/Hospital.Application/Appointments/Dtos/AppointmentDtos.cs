using Hospital.Domain.Enums;

namespace Hospital.Application.Appointments.Dtos;

public sealed record CreateAppointmentRequest(
    Guid PatientId,
    Guid TreatmentId,
    Guid? ScheduleId,
    DateOnly RequestedDate,
    string RequestedTimeSlot);

public sealed record UpdateAppointmentStatusRequest(
    AppointmentStatus Status,
    Guid? DecidedBy);

public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string PatientUhid,
    Guid TreatmentId,
    string TreatmentName,
    Guid? ScheduleId,
    DateOnly RequestedDate,
    string RequestedTimeSlot,
    AppointmentStatus Status,
    Guid? DecidedBy,
    DateTimeOffset? DecidedAt);
