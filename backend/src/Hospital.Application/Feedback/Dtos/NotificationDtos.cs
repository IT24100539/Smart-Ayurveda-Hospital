using Hospital.Domain.Enums;

namespace Hospital.Application.Communication.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    NotificationType Type,
    bool IsRead,
    DateTimeOffset CreatedAt);

public sealed record MarkAllReadResult(int Updated);
