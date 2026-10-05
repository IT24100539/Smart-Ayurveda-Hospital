namespace Hospital.Application.Abstractions;

public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string resetUrl, CancellationToken cancellationToken = default);
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
