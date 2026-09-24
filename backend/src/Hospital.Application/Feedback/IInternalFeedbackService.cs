using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.Communication;

public interface IInternalFeedbackService
{
    Task<InternalFeedbackContextDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Count of other patients' feedback in this category during the recent window.</summary>
    Task<int> CountSimilarAsync(FeedbackCategory category, Guid excludePatientId, CancellationToken cancellationToken);
}
