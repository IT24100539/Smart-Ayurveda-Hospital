using Hospital.Api.Security;
using Hospital.Application.Abstractions;
using Hospital.Application.Communication;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Hospital.Api.Hosting;

public sealed class ComplaintEscalationOptions
{
    public const string SectionName = "ComplaintEscalation";

    /// <summary>How often the worker scans for open complaints past the 5-day cutoff.</summary>
    public int IntervalHours { get; set; } = 1;
}

/// <summary>
/// Escalates complaints that have stayed Open past <see cref="ComplaintService.OverdueAfter"/>.
/// There is no separate scheduler process; this in-process timer is the whole mechanism.
/// </summary>
public sealed class ComplaintEscalationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<ComplaintEscalationOptions> _options;
    private readonly ILogger<ComplaintEscalationHostedService> _logger;

    public ComplaintEscalationHostedService(
        IServiceScopeFactory scopes,
        IOptions<ComplaintEscalationOptions> options,
        ILogger<ComplaintEscalationHostedService> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = _options.Value.IntervalHours;
        if (hours < 1)
        {
            throw new InvalidOperationException("ComplaintEscalation:IntervalHours must be at least 1.");
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceSafeAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// One scan: overdue open complaints are escalated and the assignee (or an admin) is notified.
    /// Tests call this directly instead of waiting for <see cref="PeriodicTimer"/>.
    /// </summary>
    public async Task EscalateOverdueAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var staffUsers = scope.ServiceProvider.GetRequiredService<IStaffUserRepository>();
        var adminUser = await users.FindActiveByRoleAsync(UserRole.Admin, cancellationToken);
        var adminStaff = await staffUsers.FindActiveByRoleAsync(StaffRole.Admin, cancellationToken);
        if (adminUser is null || adminStaff is null)
        {
            _logger.LogWarning("Skipping complaint auto-escalation because no active admin account is available.");
            return;
        }

        scope.ServiceProvider.GetRequiredService<CurrentUserOverride>().User = new FixedCurrentUser(adminUser);

        var complaints = scope.ServiceProvider.GetRequiredService<IComplaintService>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var cutoff = clock.UtcNow.Subtract(ComplaintService.OverdueAfter);

        var overdue = await complaints.GetOverdueComplaintsAsync(cancellationToken);
        foreach (var item in overdue)
        {
            if (item.Status != ComplaintStatus.Open || item.CreatedAt >= cutoff)
            {
                continue;
            }

            var escalated = await complaints.EscalateAsync(item.Id, cancellationToken);
            var recipientId = escalated.AssignedTo ?? adminStaff.Id;
            var subject = Truncate(escalated.Subject, 120);
            await notifications.AddAsync(new Notification
            {
                PatientId = escalated.PatientId,
                StaffUserId = recipientId,
                Title = "Complaint auto-escalated",
                Message = Truncate(
                    $"Complaint \"{subject}\" has been open for more than 5 days and was escalated for hospital review.",
                    1000),
                Type = NotificationType.ComplaintEscalated,
                IsRead = false
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Auto-escalated complaint {ComplaintId} open since {CreatedAt} and notified staff {StaffUserId}.",
                escalated.Id,
                escalated.CreatedAt,
                recipientId);
        }
    }

    private async Task RunOnceSafeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await EscalateOverdueAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complaint auto-escalation run failed.");
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
