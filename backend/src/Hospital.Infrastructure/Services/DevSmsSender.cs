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
        if (!_env.IsDevelopment())
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("[DEV SMS SENDER] To: {Phone} | Message: {Message}", toPhoneNumber, message);
        return Task.CompletedTask;
    }
}
