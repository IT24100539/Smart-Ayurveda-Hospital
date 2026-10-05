using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

internal sealed class NullPatientEventNotifier : IPatientEventNotifier
{
    public static readonly NullPatientEventNotifier Instance = new();

    public Task StageAsync(PatientNotice notice, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task DeliverAsync(PatientNotice notice, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task PublishAsync(PatientNotice notice, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class NoopUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
}

internal sealed class InMemoryNotificationRepository : INotificationRepository
{
    public List<Notification> Items { get; } = new();

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

    public Task<IReadOnlyList<Notification>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyList<Notification>)Items.Where(x => x.PatientId == patientId && x.StaffUserId == null).ToList());

    public Task<IReadOnlyList<Notification>> ListForStaffAsync(Guid staffUserId, CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyList<Notification>)Items.Where(x => x.StaffUserId == staffUserId).ToList());

    public Task<int> MarkAllReadForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        Task.FromResult(0);

    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        Items.Add(notification);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryDeviceTokenRepository : IPatientDeviceTokenRepository
{
    public List<PatientDeviceToken> Items { get; } = new();

    public Task<PatientDeviceToken?> FindByTokenAsync(string token, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(x => x.Token == token));

    public Task<IReadOnlyList<string>> ListTokensForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        Task.FromResult((IReadOnlyList<string>)Items.Where(x => x.PatientId == patientId).Select(x => x.Token).ToList());

    public Task AddAsync(PatientDeviceToken deviceToken, CancellationToken cancellationToken)
    {
        Items.Add(deviceToken);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingPushSender : IPushSender
{
    public List<PushNotification> Sent { get; } = new();

    public Task SendAsync(PushNotification push, CancellationToken cancellationToken = default)
    {
        Sent.Add(push);
        return Task.CompletedTask;
    }
}

internal sealed class ThrowingPushSender : IPushSender
{
    public Task SendAsync(PushNotification push, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("push transport unavailable");
}

internal sealed class ScriptActor : IActorContext
{
    private readonly Patient? _patient;

    public ScriptActor(Patient? patient) => _patient = patient;

    public Task<User> RequireUserAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<Patient> RequirePatientAsync(CancellationToken cancellationToken)
    {
        if (_patient is null)
        {
            throw new ForbiddenException("Only a patient can perform this action.");
        }

        return Task.FromResult(_patient);
    }

    public Task<StaffUser> RequireStaffAsync(CancellationToken cancellationToken) =>
        throw new ForbiddenException("Only staff can perform this action.");
}
