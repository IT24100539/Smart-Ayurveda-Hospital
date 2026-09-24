using Hospital.Application.Abstractions;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Communication;

public sealed class InternalFeedbackService : IInternalFeedbackService
{
    /// <summary>Window for "recent" when detecting a repeated category.</summary>
    public static readonly TimeSpan RecentWindow = TimeSpan.FromDays(30);

    private readonly IFeedbackRepository _feedback;
    private readonly IClock _clock;

    public InternalFeedbackService(IFeedbackRepository feedback, IClock clock)
    {
        _feedback = feedback;
        _clock = clock;
    }

    public async Task<InternalFeedbackContextDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var feedback = await _feedback.GetWithPatientAsync(id, cancellationToken);
        return feedback is null ? null : ToContext(feedback);
    }

    public Task<int> CountSimilarAsync(
        FeedbackCategory category,
        Guid excludePatientId,
        CancellationToken cancellationToken)
    {
        var createdAfter = _clock.UtcNow - RecentWindow;
        return _feedback.CountRecentInCategoryExcludingPatientAsync(
            category,
            excludePatientId,
            createdAfter,
            cancellationToken);
    }

    private static InternalFeedbackContextDto ToContext(Feedback feedback)
    {
        var patient = feedback.Patient;
        var name = patient is null
            ? feedback.PatientNameSnapshot
            : $"{patient.FirstName} {patient.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = feedback.PatientNameSnapshot;
        }

        return new InternalFeedbackContextDto(
            feedback.Id,
            feedback.PatientId,
            name,
            patient?.Uhid,
            patient?.Prakriti.ToString(),
            patient?.Vikriti.ToString(),
            feedback.Rating,
            feedback.Comment,
            feedback.IsAnonymous,
            feedback.AppointmentId,
            feedback.TreatmentId);
    }
}
