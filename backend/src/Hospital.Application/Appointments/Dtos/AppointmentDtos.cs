using Hospital.Domain.Enums;

namespace Hospital.Application.Appointments.Dtos;

public sealed record CreateAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Reason,
    string? Notes);

public sealed record UpdateAppointmentStatusRequest(
    AppointmentStatus Status,
    string? CancellationReason);

public sealed record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string PatientUhid,
    Guid DoctorId,
    string DoctorName,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    AppointmentStatus Status,
    string Reason,
    string? Notes);
