using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Hospital.Application.Communication;

public sealed class PatientEventNotifier : IPatientEventNotifier
{
    public const int TitleMaxLength = 160;
    public const int MessageMaxLength = 1000;

    private readonly INotificationRepository _notifications;
    private readonly IPatientDeviceTokenRepository _tokens;
    private readonly IPushSender _push;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PatientEventNotifier> _logger;

    public PatientEventNotifier(
        INotificationRepository notifications,
        IPatientDeviceTokenRepository tokens,
        IPushSender push,
        IUnitOfWork unitOfWork,
        ILogger<PatientEventNotifier> logger)
    {
        _notifications = notifications;
        _tokens = tokens;
        _push = push;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task StageAsync(PatientNotice notice, CancellationToken cancellationToken)
    {
        var (title, message) = Normalize(notice);
        return _notifications.AddAsync(new Notification
        {
            PatientId = notice.PatientId,
            Title = title,
            Message = message,
            Type = notice.Type,
            IsRead = false
        }, cancellationToken);
    }

    public async Task DeliverAsync(PatientNotice notice, CancellationToken cancellationToken)
    {
        var (title, message) = Normalize(notice);
        var tokens = await _tokens.ListTokensForPatientAsync(notice.PatientId, cancellationToken);
        if (tokens.Count == 0)
        {
            return;
        }

        try
        {
            await _push.SendAsync(
                new PushNotification(notice.PatientId, tokens, title, message, notice.Type),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Push delivery failed for patient {PatientId} event {EventType}. The notice stays in the inbox.",
                notice.PatientId,
                notice.Type);
        }
    }

    public async Task PublishAsync(PatientNotice notice, CancellationToken cancellationToken)
    {
        await StageAsync(notice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await DeliverAsync(notice, cancellationToken);
    }

    private static (string Title, string Message) Normalize(PatientNotice notice)
    {
        var title = notice.Title.Trim();
        var message = notice.Message.Trim();
        if (title.Length == 0 || message.Length == 0)
        {
            throw new DomainException("A patient notice needs a title and a message.");
        }

        if (title.Length > TitleMaxLength)
        {
            title = title[..TitleMaxLength];
        }

        if (message.Length > MessageMaxLength)
        {
            message = message[..MessageMaxLength];
        }

        return (title, message);
    }
}
