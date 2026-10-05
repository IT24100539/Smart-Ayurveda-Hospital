using Hospital.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

/// <summary>
/// Development stand-in for Firebase Cloud Messaging.
/// Logs the notice only when the host environment is Development.
/// FCM is not called. Do not put a Firebase service account in git.
/// </summary>
public sealed class DevPushSender : IPushSender
{
    private readonly ILogger<DevPushSender> _logger;
    private readonly IHostEnvironment _env;

    public DevPushSender(ILogger<DevPushSender> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task SendAsync(PushNotification push, CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "[DEV PUSH SENDER] Patient {PatientId} | Event {EventType} | Tokens {TokenCount} | Title {Title} | Body {Body}",
            push.PatientId,
            push.EventType,
            push.DeviceTokens.Count,
            push.Title,
            push.Body);
        return Task.CompletedTask;
    }
}
