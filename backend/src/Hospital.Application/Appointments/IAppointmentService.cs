using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;

namespace Hospital.Application.Appointments;

public interface IAppointmentService
{
    Task<AppointmentDto> CreateAsync(CreateAppointmentRequest request, CancellationToken cancellationToken);
    Task<AppointmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<AppointmentDto>> ListAsync(DateOnly? onDate, Guid? patientId, Guid? treatmentId, int page, int pageSize, CancellationToken cancellationToken);
    Task<AppointmentDto> UpdateStatusAsync(Guid id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken);
    Task CancelAsync(Guid appointmentId, Guid requestingPatientId, CancellationToken cancellationToken);
}
