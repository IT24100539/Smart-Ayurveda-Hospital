using Hospital.Domain.Enums;

namespace Hospital.Application.Communication.Dtos;

public sealed record CreateFeedbackRequest(
    Guid? AppointmentId,
    Guid? TreatmentId,
    int Rating,
    string Comment,
    bool IsAnonymous);

public sealed record UpdateFeedbackRequest(
    int? Rating,
    string? Comment,
    bool? IsAnonymous,
    bool Withdraw);

/// <summary>Staff show/hide. Values start at 1 so a missing body cannot default to show.</summary>
public enum FeedbackModerationAction
{
    Show = 1,
    Hide = 2
}

public sealed record ModerateFeedbackRequest(FeedbackModerationAction Action);

public sealed record FeedbackSearchQuery(
    FeedbackStatus? Status,
    int? Rating,
    FeedbackCategory? Category,
    FeedbackSentiment? Sentiment,
    int Page = 1,
    int PageSize = 20,
    string? Sort = null,
    string? Search = null);

/// <summary>The signed-in patient's own comments, including ones still in moderation.</summary>
public sealed record PatientFeedbackDto(
    Guid Id,
    Guid? AppointmentId,
    Guid? TreatmentId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    FeedbackSentiment? Sentiment,
    FeedbackCategory? Category,
    FeedbackStatus Status,
    DateTimeOffset CreatedAt,
    bool CanEdit);

public sealed record FeedbackCountDto(string Key, int Count);

public sealed record FeedbackStatsDto(
    int Total,
    double AverageRating,
    int PendingModeration,
    int Negative,
    IReadOnlyList<FeedbackCountDto> ByStatus,
    IReadOnlyList<FeedbackCountDto> ByCategory,
    IReadOnlyList<FeedbackCountDto> BySentiment);

public sealed record FeedbackSummaryDto(
    Guid Id,
    Guid? PatientId,
    string PatientName,
    Guid? AppointmentId,
    Guid? TreatmentId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    FeedbackSentiment? Sentiment,
    FeedbackCategory? Category,
    FeedbackStatus Status,
    int LikeCount,
    int DislikeCount,
    int PostedReplyCount,
    DateTimeOffset CreatedAt);

/// <summary>
/// A posted reply on the public board. Account ids are omitted so a reaction
/// thread cannot be joined back to a login, including on anonymous comments.
/// </summary>
public sealed record PublicReplyDto(
    Guid Id,
    FeedbackReplyUserRole UserRole,
    string Reply,
    DateTimeOffset CreatedAt);

/// <summary>
/// Approved public comment, including only replies that have been posted.
/// </summary>
public sealed record PublicFeedbackDto(
    Guid Id,
    Guid? PatientId,
    string PatientName,
    Guid? AppointmentId,
    Guid? TreatmentId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    FeedbackSentiment? Sentiment,
    FeedbackCategory? Category,
    FeedbackStatus Status,
    int LikeCount,
    int DislikeCount,
    int PostedReplyCount,
    IReadOnlyList<PublicReplyDto> Replies,
    DateTimeOffset CreatedAt);

public sealed record FeedbackDetailDto(
    Guid Id,
    Guid? PatientId,
    string PatientName,
    Guid? AppointmentId,
    Guid? TreatmentId,
    int Rating,
    string Comment,
    bool IsAnonymous,
    FeedbackSentiment? Sentiment,
    FeedbackCategory? Category,
    FeedbackStatus Status,
    Guid? ModeratedBy,
    DateTimeOffset? ModeratedAt,
    int LikeCount,
    int DislikeCount,
    IReadOnlyList<ReplyDto> Replies,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
