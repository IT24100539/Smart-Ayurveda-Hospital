using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Abstractions;

public interface IFeedbackRepository
{
    Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(Feedback feedback, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Feedback> Items, int Total)> SearchAsync(
        FeedbackStatus? status,
        int? rating,
        FeedbackCategory? category,
        FeedbackSentiment? sentiment,
        int page,
        int pageSize,
        string? sort,
        string? search,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Feedback>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Feedback>> ListVisibleAsync(int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<FeedbackStatRow>> ListForStatsAsync(CancellationToken cancellationToken);

    /// <summary>Agent read. Includes the patient so prakriti/vikriti can be returned without a second query.</summary>
    Task<Feedback?> GetWithPatientAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Other patients' feedback in the same category, created on or after <paramref name="createdAfter"/>.
    /// The current patient's own rows are excluded so a repeat is someone else's report.
    /// </summary>
    Task<int> CountRecentInCategoryExcludingPatientAsync(
        FeedbackCategory category,
        Guid excludePatientId,
        DateTimeOffset createdAfter,
        CancellationToken cancellationToken);
}

public interface IFeedbackReactionRepository
{
    Task<FeedbackReaction?> GetByUserAsync(Guid feedbackId, Guid userId, CancellationToken cancellationToken);
    Task AddAsync(FeedbackReaction reaction, CancellationToken cancellationToken);
    void Remove(FeedbackReaction reaction);
}

/// <summary>Projection used by the staff feedback statistics card.</summary>
public sealed record FeedbackStatRow(
    FeedbackStatus Status,
    FeedbackSentiment? Sentiment,
    FeedbackCategory? Category,
    int Rating);

public interface IFeedbackReplyRepository
{
    Task<FeedbackReply?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(FeedbackReply reply, CancellationToken cancellationToken);
}

public interface IComplaintRepository
{
    Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Complaint>> ListAsync(
        ComplaintStatus? status,
        ComplaintPriority? priority,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Complaint>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Complaint>> ListOpenOlderThanAsync(DateTimeOffset createdBefore, CancellationToken cancellationToken);
    Task AddAsync(Complaint complaint, CancellationToken cancellationToken);
}

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> ListForStaffAsync(Guid staffUserId, CancellationToken cancellationToken);
    Task<int> MarkAllReadForPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
}
