using Hospital.Domain.Enums;

namespace Hospital.Application.Abstractions;

public interface IPushSender
{
    Task SendAsync(PushNotification push, CancellationToken cancellationToken = default);
}

/// <summary>
/// One stored patient notice ready for a push transport.
/// Device tokens are delivery addresses, not something to write to logs.
/// </summary>
public sealed record PushNotification(
    Guid PatientId,
    IReadOnlyList<string> DeviceTokens,
    string Title,
    string Body,
    NotificationType EventType);
