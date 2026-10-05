using Hospital.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

public sealed class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;
    private readonly IHostEnvironment _env;

    public DevEmailSender(ILogger<DevEmailSender> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl, CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "[DEV EMAIL SENDER] Password reset link for {Email}: {ResetUrl}?token={Token}&email={EmailUrlEncoded}",
            toEmail, resetUrl, resetToken, Uri.EscapeDataString(toEmail));
        return Task.CompletedTask;
    }

    public Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_env.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("[DEV EMAIL SENDER] To: {Email} | Subject: {Subject} | Body: {Body}", toEmail, subject, body);
        return Task.CompletedTask;
    }
}
