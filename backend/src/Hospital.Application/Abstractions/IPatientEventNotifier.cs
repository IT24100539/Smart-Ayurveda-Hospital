using Hospital.Domain.Enums;

namespace Hospital.Application.Abstractions;

public interface IPatientEventNotifier
{
    /// <summary>Tracks an inbox row. The caller saves it with the clinical change.</summary>
    Task StageAsync(PatientNotice notice, CancellationToken cancellationToken);

    /// <summary>Pushes a notice that is already stored. A transport failure does not undo the clinical change.</summary>
    Task DeliverAsync(PatientNotice notice, CancellationToken cancellationToken);

    /// <summary>Stores the notice and then tries to push it.</summary>
    Task PublishAsync(PatientNotice notice, CancellationToken cancellationToken);
}

public sealed record PatientNotice(
    Guid PatientId,
    NotificationType Type,
    string Title,
    string Message);
