using Hospital.Application.Abstractions;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hospital.IntegrationTests;

public sealed class DevPushSenderTests
{
    [Fact]
    public async Task Send_LogsOnlyInDevelopment_AndDoesNotLogTheDeviceToken()
    {
        var logger = new ListLogger<DevPushSender>();
        var push = new PushNotification(
            Guid.NewGuid(),
            new[] { "secret-device-token" },
            "Appointment approved",
            "Your Abhyanga visit is approved.",
            NotificationType.AppointmentApproved);

        await new DevPushSender(logger, new TestHostEnvironment(Environments.Development))
            .SendAsync(push, CancellationToken.None);
        await new DevPushSender(logger, new TestHostEnvironment(Environments.Production))
            .SendAsync(push, CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("DEV PUSH SENDER", entry);
        Assert.Contains("Appointment approved", entry);
        Assert.DoesNotContain("secret-device-token", entry);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName) => EnvironmentName = environmentName;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Hospital.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(formatter(state, exception));
        }
    }
}
