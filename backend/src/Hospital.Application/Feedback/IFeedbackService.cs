using Hospital.Application.Common;
using Hospital.Application.Communication.Dtos;

namespace Hospital.Application.Communication;

public interface IFeedbackService
{
    Task<FeedbackDetailDto> CreateAsync(CreateFeedbackRequest request, CancellationToken cancellationToken);

    /// <summary>Patient edits or withdraws their own feedback within 24 hours of CreatedAt.</summary>
    Task<FeedbackDetailDto> EditAsync(Guid id, UpdateFeedbackRequest request, CancellationToken cancellationToken);

    Task<PagedResult<FeedbackSummaryDto>> SearchAsync(FeedbackSearchQuery query, CancellationToken cancellationToken);

    /// <summary>Staff detail, including unposted AI drafts and who last moderated the comment.</summary>
    Task<FeedbackDetailDto> GetForStaffAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Staff or admin shows or hides a comment and records who moderated it.</summary>
    Task<FeedbackDetailDto> ModerateAsync(Guid id, FeedbackModerationAction action, CancellationToken cancellationToken);

    /// <summary>Patient-facing feed. Only <see cref="Hospital.Domain.Enums.FeedbackStatus.Visible"/> rows, with anonymity applied and posted replies included.</summary>
    Task<IReadOnlyList<PublicFeedbackDto>> GetPublicFeedAsync(CancellationToken cancellationToken);

    /// <summary>Comments written by the signed-in patient, including hidden and pending rows.</summary>
    Task<IReadOnlyList<PatientFeedbackDto>> ListMineAsync(CancellationToken cancellationToken);

    /// <summary>Counts across all stored feedback, for the staff dashboard.</summary>
    Task<FeedbackStatsDto> GetStatsAsync(CancellationToken cancellationToken);
}
