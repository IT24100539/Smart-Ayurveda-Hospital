using Hospital.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hospital.Infrastructure.Services;

public sealed class DevSmsSender : ISmsSender
{
    private readonly ILogger<DevSmsSender> _logger;
    private readonly IHostEnvironment _env;

    public DevSmsSender(ILogger<DevSmsSender> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation("[DEV SMS SENDER] To: {Phone} | Message: {Message}", toPhoneNumber, message);
        }
        else
        {
            _logger.LogInformation("[PROD SMS SENDER] SMS dispatched to {Phone}", toPhoneNumber);
        }

        return Task.CompletedTask;
    }
}
