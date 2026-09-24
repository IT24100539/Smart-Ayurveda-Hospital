using Hospital.Application.Abstractions;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Communication;

public sealed class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly IActorContext _actors;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        INotificationRepository notifications,
        IActorContext actors,
        IUnitOfWork unitOfWork)
    {
        _notifications = notifications;
        _actors = actors;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForPatient(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        if (patient.Id != patientId)
        {
            throw new ForbiddenException("You can only read your own notifications.");
        }

        var items = await _notifications.ListForPatientAsync(patient.Id, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<NotificationDto> MarkReadAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var notification = await _notifications.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), id);
        if (notification.PatientId != patient.Id || notification.StaffUserId is not null)
        {
            throw new ForbiddenException("You can only update your own notifications.");
        }

        notification.IsRead = true;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(notification);
    }

    public async Task<MarkAllReadResult> MarkAllReadAsync(CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var updated = await _notifications.MarkAllReadForPatientAsync(patient.Id, cancellationToken);
        if (updated > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new MarkAllReadResult(updated);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetForStaffAsync(CancellationToken cancellationToken)
    {
        var staff = await _actors.RequireStaffAsync(cancellationToken);
        var items = await _notifications.ListForStaffAsync(staff.Id, cancellationToken);
        return items.Select(Map).ToList();
    }

    private static NotificationDto Map(Notification notification) => new(
        notification.Id,
        notification.Title,
        notification.Message,
        notification.Type,
        notification.IsRead,
        notification.CreatedAt);
}
