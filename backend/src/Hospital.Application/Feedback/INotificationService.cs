using Hospital.Application.Communication.Dtos;

namespace Hospital.Application.Communication;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForPatient(Guid patientId, CancellationToken cancellationToken);
    Task<NotificationDto> MarkReadAsync(Guid id, CancellationToken cancellationToken);
    Task<MarkAllReadResult> MarkAllReadAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationDto>> GetForStaffAsync(CancellationToken cancellationToken);
}
