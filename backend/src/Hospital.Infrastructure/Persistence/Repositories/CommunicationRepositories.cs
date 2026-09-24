using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hospital.Infrastructure.Persistence.Repositories;

public sealed class FeedbackRepository : IFeedbackRepository
{
    private readonly HospitalDbContext _db;

    public FeedbackRepository(HospitalDbContext db) => _db = db;

    public Task<Feedback?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Feedbacks
            .Include(x => x.Reactions)
            .Include(x => x.Replies)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Feedback feedback, CancellationToken cancellationToken) =>
        await _db.Feedbacks.AddAsync(feedback, cancellationToken);

    public async Task<(IReadOnlyList<Feedback> Items, int Total)> SearchAsync(
        FeedbackStatus? status,
        int? rating,
        FeedbackCategory? category,
        FeedbackSentiment? sentiment,
        int page,
        int pageSize,
        string? sort,
        string? search,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = _db.Feedbacks.AsNoTracking()
            .Include(x => x.Reactions)
            .Include(x => x.Replies)
            .AsQueryable();

        if (status is not null)
        {
            q = q.Where(x => x.Status == status);
        }

        if (rating is not null)
        {
            q = q.Where(x => x.Rating == rating);
        }

        if (category is not null)
        {
            q = q.Where(x => x.Category == category);
        }

        if (sentiment is not null)
        {
            q = q.Where(x => x.Sentiment == sentiment);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            q = q.Where(x =>
                x.Comment.ToLower().Contains(term) ||
                (!x.IsAnonymous && x.PatientNameSnapshot.ToLower().Contains(term)));
        }

        var sortKey = (sort ?? string.Empty).Trim().ToLowerInvariant();
        var ordered = sortKey switch
        {
            "rating" or "ratingasc" => q.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAt),
            "ratingdesc" => q.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt),
            "sentiment" => q.OrderBy(x => x.Sentiment).ThenByDescending(x => x.CreatedAt),
            "sentimentdesc" => q.OrderByDescending(x => x.Sentiment).ThenByDescending(x => x.CreatedAt),
            "createdat" or "createdatasc" => q.OrderBy(x => x.CreatedAt),
            _ => q.OrderByDescending(x => x.CreatedAt)
        };

        var total = await ordered.CountAsync(cancellationToken);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<Feedback>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        await _db.Feedbacks.AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FeedbackStatRow>> ListForStatsAsync(CancellationToken cancellationToken) =>
        await _db.Feedbacks.AsNoTracking()
            .Select(x => new FeedbackStatRow(x.Status, x.Sentiment, x.Category, x.Rating))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Feedback>> ListVisibleAsync(int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 100);
        return await _db.Feedbacks.AsNoTracking()
            .Include(x => x.Reactions)
            .Include(x => x.Replies)
            .Where(x => x.Status == FeedbackStatus.Visible)
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task<Feedback?> GetWithPatientAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Feedbacks.AsNoTracking()
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<int> CountRecentInCategoryExcludingPatientAsync(
        FeedbackCategory category,
        Guid excludePatientId,
        DateTimeOffset createdAfter,
        CancellationToken cancellationToken) =>
        _db.Feedbacks.AsNoTracking()
            .CountAsync(
                x => x.Category == category
                    && x.PatientId != excludePatientId
                    && x.CreatedAt >= createdAfter,
                cancellationToken);
}

public sealed class FeedbackReactionRepository : IFeedbackReactionRepository
{
    private readonly HospitalDbContext _db;

    public FeedbackReactionRepository(HospitalDbContext db) => _db = db;

    public Task<FeedbackReaction?> GetByUserAsync(Guid feedbackId, Guid userId, CancellationToken cancellationToken) =>
        _db.FeedbackReactions.FirstOrDefaultAsync(
            x => x.FeedbackId == feedbackId && x.UserId == userId,
            cancellationToken);

    public async Task AddAsync(FeedbackReaction reaction, CancellationToken cancellationToken) =>
        await _db.FeedbackReactions.AddAsync(reaction, cancellationToken);

    public void Remove(FeedbackReaction reaction) => _db.FeedbackReactions.Remove(reaction);
}

public sealed class FeedbackReplyRepository : IFeedbackReplyRepository
{
    private readonly HospitalDbContext _db;

    public FeedbackReplyRepository(HospitalDbContext db) => _db = db;

    public Task<FeedbackReply?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.FeedbackReplies
            .Include(x => x.Feedback)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(FeedbackReply reply, CancellationToken cancellationToken) =>
        await _db.FeedbackReplies.AddAsync(reply, cancellationToken);
}

public sealed class ComplaintRepository : IComplaintRepository
{
    private readonly HospitalDbContext _db;

    public ComplaintRepository(HospitalDbContext db) => _db = db;

    public Task<Complaint?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Complaints
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Complaint>> ListAsync(
        ComplaintStatus? status,
        ComplaintPriority? priority,
        CancellationToken cancellationToken)
    {
        var query = _db.Complaints.AsNoTracking().Include(x => x.Patient).AsQueryable();
        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(x => x.Priority == priority);
        }

        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Complaint>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        await _db.Complaints.AsNoTracking()
            .Include(x => x.Patient)
            .Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Complaint>> ListOpenOlderThanAsync(
        DateTimeOffset createdBefore,
        CancellationToken cancellationToken) =>
        await _db.Complaints.AsNoTracking()
            .Include(x => x.Patient)
            .Where(x => x.Status == ComplaintStatus.Open && x.CreatedAt < createdBefore)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Complaint complaint, CancellationToken cancellationToken) =>
        await _db.Complaints.AddAsync(complaint, cancellationToken);
}

public sealed class NotificationRepository : INotificationRepository
{
    private readonly HospitalDbContext _db;

    public NotificationRepository(HospitalDbContext db) => _db = db;

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken) =>
        await _db.Notifications.AsNoTracking()
            .Where(x => x.PatientId == patientId && x.StaffUserId == null)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListForStaffAsync(Guid staffUserId, CancellationToken cancellationToken) =>
        await _db.Notifications.AsNoTracking()
            .Where(x => x.StaffUserId == staffUserId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

    public async Task<int> MarkAllReadForPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var unread = await _db.Notifications
            .Where(x => x.PatientId == patientId && x.StaffUserId == null && !x.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var item in unread)
        {
            item.IsRead = true;
        }

        return unread.Count;
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken) =>
        await _db.Notifications.AddAsync(notification, cancellationToken);
}

public sealed class TreatmentCatalog : ITreatmentCatalog
{
    private readonly HospitalDbContext _db;

    public TreatmentCatalog(HospitalDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid treatmentId, CancellationToken cancellationToken) =>
        _db.Treatments.AnyAsync(x => x.Id == treatmentId, cancellationToken);
}
