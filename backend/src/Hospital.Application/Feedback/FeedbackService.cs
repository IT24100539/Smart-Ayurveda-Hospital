using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Common;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Communication;

public sealed class FeedbackService : IFeedbackService
{
    public static readonly TimeSpan EditWindow = TimeSpan.FromHours(24);
    private const int PublicFeedSize = 50;

    private readonly IFeedbackRepository _feedback;
    private readonly IAppointmentService _appointments;
    private readonly ITreatmentCatalog _treatments;
    private readonly IActorContext _actors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IReplyService _replies;

    public FeedbackService(
        IFeedbackRepository feedback,
        IAppointmentService appointments,
        ITreatmentCatalog treatments,
        IActorContext actors,
        IUnitOfWork unitOfWork,
        IClock clock,
        IReplyService replies)
    {
        _feedback = feedback;
        _appointments = appointments;
        _treatments = treatments;
        _actors = actors;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _replies = replies;
    }

    public async Task<FeedbackDetailDto> CreateAsync(CreateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        if (request.Rating is < 1 or > 5)
        {
            throw new DomainException("Rating must be between 1 and 5.");
        }

        if (string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new DomainException("Comment is required.");
        }

        // Default (stricter): an AppointmentId is accepted only when that visit belongs to this
        // patient and Member 3's appointment Status is Completed. IAppointmentService is that
        // read — the same contract the appointments API uses.
        //
        // Fallback: a general comment with no visit may be linked only to a valid TreatmentId.
        // No appointment status check runs on that path. When both ids are present, the
        // completed-visit rule still applies and the treatment is only checked for existence.
        if (request.AppointmentId is Guid appointmentId)
        {
            var appointment = await _appointments.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment.PatientId != patient.Id)
            {
                throw new DomainException("Feedback can only be linked to your own appointment.");
            }

            if (appointment.Status != AppointmentStatus.Completed)
            {
                throw new DomainException("Feedback linked to an appointment is accepted only after that visit is completed.");
            }
        }

        if (request.TreatmentId is Guid treatmentId)
        {
            if (!await _treatments.ExistsAsync(treatmentId, cancellationToken))
            {
                throw new NotFoundException(nameof(Treatment), treatmentId);
            }
        }
        else if (request.AppointmentId is null)
        {
            throw new DomainException("Link feedback to a completed appointment, or to a treatment for a general comment.");
        }

        var name = $"{patient.FirstName} {patient.LastName}".Trim();
        if (name.Length > 160)
        {
            name = name[..160];
        }

        var feedback = new Feedback
        {
            PatientId = patient.Id,
            PatientNameSnapshot = name,
            AppointmentId = request.AppointmentId,
            TreatmentId = request.TreatmentId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            IsAnonymous = request.IsAnonymous,
            Status = FeedbackStatus.PendingModeration
        };

        await _feedback.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // The row is committed before the agent runs. A timeout or bad payload must not undo it.
        await _replies.TryCaptureAnalysisAsync(feedback, cancellationToken);
        return FeedbackMapper.ToDetail(feedback, includeUnpostedReplies: false);
    }

    public async Task<FeedbackDetailDto> EditAsync(Guid id, UpdateFeedbackRequest request, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var feedback = await _feedback.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), id);

        if (feedback.PatientId != patient.Id)
        {
            throw new ForbiddenException("You can only edit your own feedback.");
        }

        if (_clock.UtcNow > feedback.CreatedAt.Add(EditWindow))
        {
            throw new DomainException("Feedback can only be edited or withdrawn within 24 hours of submission.");
        }

        var hasChange = request.Withdraw || request.Rating is not null || request.Comment is not null || request.IsAnonymous is not null;
        if (!hasChange)
        {
            throw new DomainException("Provide a change or withdraw the feedback.");
        }

        if (request.Rating is < 1 or > 5)
        {
            throw new DomainException("Rating must be between 1 and 5.");
        }

        if (request.Rating is int rating)
        {
            feedback.Rating = rating;
        }

        if (request.Comment is not null)
        {
            var comment = request.Comment.Trim();
            if (comment.Length == 0)
            {
                throw new DomainException("Comment is required.");
            }

            feedback.Comment = comment;
        }

        if (request.IsAnonymous is bool isAnonymous)
        {
            feedback.IsAnonymous = isAnonymous;
        }

        if (request.Withdraw)
        {
            feedback.Status = FeedbackStatus.Hidden;
        }
        else
        {
            feedback.Status = FeedbackStatus.PendingModeration;
            feedback.ModeratedById = null;
            feedback.ModeratedAt = null;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FeedbackMapper.ToDetail(feedback, includeUnpostedReplies: false);
    }

    public async Task<PagedResult<FeedbackSummaryDto>> SearchAsync(FeedbackSearchQuery query, CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        if (query.Rating is < 1 or > 5)
        {
            throw new DomainException("Rating filter must be between 1 and 5.");
        }

        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _feedback.SearchAsync(
            query.Status,
            query.Rating,
            query.Category,
            query.Sentiment,
            page,
            pageSize,
            query.Sort,
            query.Search,
            cancellationToken);

        return new PagedResult<FeedbackSummaryDto>
        {
            Items = items.Select(FeedbackMapper.ToSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FeedbackDetailDto> GetForStaffAsync(Guid id, CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var feedback = await _feedback.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), id);
        return FeedbackMapper.ToDetail(feedback, includeUnpostedReplies: true);
    }

    public async Task<FeedbackDetailDto> ModerateAsync(
        Guid id,
        FeedbackModerationAction action,
        CancellationToken cancellationToken)
    {
        if (action is not (FeedbackModerationAction.Show or FeedbackModerationAction.Hide))
        {
            throw new DomainException("Moderation must show or hide the feedback.");
        }

        var staff = await _actors.RequireStaffAsync(cancellationToken);
        var feedback = await _feedback.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), id);

        feedback.Status = action == FeedbackModerationAction.Hide
            ? FeedbackStatus.Hidden
            : FeedbackStatus.Visible;
        feedback.ModeratedById = staff.Id;
        feedback.ModeratedAt = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FeedbackMapper.ToDetail(feedback, includeUnpostedReplies: true);
    }

    public async Task<IReadOnlyList<PublicFeedbackDto>> GetPublicFeedAsync(CancellationToken cancellationToken)
    {
        var items = await _feedback.ListVisibleAsync(PublicFeedSize, cancellationToken);
        return items.Select(FeedbackMapper.ToPublic).ToList();
    }

    public async Task<IReadOnlyList<PatientFeedbackDto>> ListMineAsync(CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var items = await _feedback.ListForPatientAsync(patient.Id, cancellationToken);
        return items.Select(item => new PatientFeedbackDto(
            item.Id,
            item.AppointmentId,
            item.TreatmentId,
            item.Rating,
            item.Comment,
            item.IsAnonymous,
            item.Sentiment,
            item.Category,
            item.Status,
            item.CreatedAt,
            _clock.UtcNow <= item.CreatedAt.Add(EditWindow))).ToList();
    }

    public async Task<FeedbackStatsDto> GetStatsAsync(CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var rows = await _feedback.ListForStatsAsync(cancellationToken);
        var total = rows.Count;
        var average = total == 0 ? 0 : Math.Round(rows.Average(x => x.Rating), 2);
        return new FeedbackStatsDto(
            total,
            average,
            rows.Count(x => x.Status == FeedbackStatus.PendingModeration),
            rows.Count(x => x.Sentiment == FeedbackSentiment.Negative),
            Count(rows, x => x.Status.ToString()),
            Count(rows.Where(x => x.Category is not null), x => x.Category!.Value.ToString()),
            Count(rows.Where(x => x.Sentiment is not null), x => x.Sentiment!.Value.ToString()));
    }

    private static IReadOnlyList<FeedbackCountDto> Count<T>(IEnumerable<T> rows, Func<T, string> key) =>
        rows.GroupBy(key)
            .OrderByDescending(group => group.Count())
            .Select(group => new FeedbackCountDto(group.Key, group.Count()))
            .ToList();
}
