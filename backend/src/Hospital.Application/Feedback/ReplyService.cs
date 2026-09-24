using Hospital.Application.Abstractions;
using Hospital.Application.Agents;
using Hospital.Application.Agents.Dtos;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Communication;

public sealed class ReplyService : IReplyService
{
    private readonly IFeedbackRepository _feedback;
    private readonly IFeedbackReplyRepository _replies;
    private readonly INotificationRepository _notifications;
    private readonly IActorContext _actors;
    private readonly IAgentClient _agents;
    private readonly IStaffUserRepository _staffUsers;
    private readonly IUnitOfWork _unitOfWork;

    public ReplyService(
        IFeedbackRepository feedback,
        IFeedbackReplyRepository replies,
        INotificationRepository notifications,
        IActorContext actors,
        IAgentClient agents,
        IStaffUserRepository staffUsers,
        IUnitOfWork unitOfWork)
    {
        _feedback = feedback;
        _replies = replies;
        _notifications = notifications;
        _actors = actors;
        _agents = agents;
        _staffUsers = staffUsers;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReplyDto> CreateManualReplyAsync(
        Guid feedbackId,
        CreateReplyRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        await _actors.RequireStaffAsync(cancellationToken);
        var feedback = await _feedback.GetByIdAsync(feedbackId, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);

        var reply = new FeedbackReply
        {
            FeedbackId = feedback.Id,
            UserId = user.Id,
            UserRole = FeedbackReplyUserRole.Staff,
            Reply = request.Reply.Trim(),
            IsAiGenerated = false,
            Status = FeedbackReplyStatus.Posted
        };

        await _replies.AddAsync(reply, cancellationToken);
        await NotifyPatientAsync(feedback, reply.Reply, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FeedbackMapper.ToReply(reply);
    }

    public async Task<ReplyDto> CreatePatientReplyAsync(
        Guid feedbackId,
        CreateReplyRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        await _actors.RequirePatientAsync(cancellationToken);
        var feedback = await _feedback.GetByIdAsync(feedbackId, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        if (feedback.Status != FeedbackStatus.Visible)
        {
            throw new DomainException("Replies are only allowed on visible feedback.");
        }

        var reply = new FeedbackReply
        {
            FeedbackId = feedback.Id,
            UserId = user.Id,
            UserRole = FeedbackReplyUserRole.Patient,
            Reply = request.Reply.Trim(),
            IsAiGenerated = false,
            Status = FeedbackReplyStatus.Posted
        };

        await _replies.AddAsync(reply, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FeedbackMapper.ToReply(reply);
    }

    public async Task TryCaptureAnalysisAsync(Feedback feedback, CancellationToken cancellationToken)
    {
        try
        {
            var response = await CallAgentAsync(feedback, cancellationToken);
            await StoreAnalysisAsync(feedback, response, requireDraft: false, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Submission already succeeded. Staff can still moderate and reply by hand.
        }
    }

    public async Task<ReplyDto> RequestAiDraftAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var feedback = await _feedback.GetByIdAsync(feedbackId, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);

        // The feedback row is already stored. If this call fails, that comment stays.
        var response = await CallAgentAsync(feedback, cancellationToken);
        var draft = await StoreAnalysisAsync(feedback, response, requireDraft: true, cancellationToken);
        return FeedbackMapper.ToReply(draft!);
    }

    public async Task<ReplyDto> DecideDraftAsync(Guid id, ReplyDecisionRequest request, CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        await _actors.RequireStaffAsync(cancellationToken);
        var reply = await _replies.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(FeedbackReply), id);

        if (!reply.IsAiGenerated || reply.Status != FeedbackReplyStatus.Draft)
        {
            throw new DomainException("Only an AI draft can be approved, edited, or rejected. The agent never publishes a reply on its own.");
        }

        if (request.Decision == ReplyDecision.Reject)
        {
            reply.Status = FeedbackReplyStatus.Rejected;
            reply.UserId = user.Id;
            reply.UserRole = FeedbackReplyUserRole.Staff;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return FeedbackMapper.ToReply(reply);
        }

        if (request.Decision == ReplyDecision.Save)
        {
            if (string.IsNullOrWhiteSpace(request.Reply))
            {
                throw new DomainException("An edited reply needs text.");
            }

            reply.Reply = request.Reply.Trim();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return FeedbackMapper.ToReply(reply);
        }

        if (request.Decision == ReplyDecision.Edit)
        {
            if (string.IsNullOrWhiteSpace(request.Reply))
            {
                throw new DomainException("An edited reply needs the text to publish.");
            }

            reply.Reply = request.Reply.Trim();
        }
        else if (request.Decision == ReplyDecision.Approve)
        {
            if (!string.IsNullOrWhiteSpace(request.Reply))
            {
                reply.Reply = request.Reply.Trim();
            }
        }
        else
        {
            throw new DomainException("Unknown reply decision.");
        }

        await PublishAsync(reply, user.Id, cancellationToken);
        return FeedbackMapper.ToReply(reply);
    }

    private async Task<FeedbackSupportAgentResponse> CallAgentAsync(Feedback feedback, CancellationToken cancellationToken)
    {
        try
        {
            return await _agents.DraftFeedbackSupportAsync(
                new FeedbackSupportAgentRequest(feedback.Id, feedback.Comment, feedback.PatientId),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new DomainException(
                "The support agent is unavailable. This feedback stays on record, and you can reply manually.");
        }
    }

    /// <summary>
    /// Writes sentiment, category, an unpublished draft, and a staff alert.
    /// Returns the new draft, or null when the agent skipped drafting and a draft was not required.
    /// </summary>
    private async Task<FeedbackReply?> StoreAnalysisAsync(
        Feedback feedback,
        FeedbackSupportAgentResponse response,
        bool requireDraft,
        CancellationToken cancellationToken)
    {
        ApplyAnalysis(feedback, response);

        FeedbackReply? draft = null;
        var text = response.SuggestedReply?.Trim();
        if (!response.DraftSkipped && !string.IsNullOrWhiteSpace(text))
        {
            if (text.Length > 2000)
            {
                text = text[..2000];
            }

            // The agent never publishes. Status stays Draft until staff approves it.
            draft = new FeedbackReply
            {
                FeedbackId = feedback.Id,
                UserId = null,
                UserRole = FeedbackReplyUserRole.Staff,
                Reply = text,
                IsAiGenerated = true,
                Status = FeedbackReplyStatus.Draft
            };
            await _replies.AddAsync(draft, cancellationToken);
        }

        if (NeedsStaffAlert(response))
        {
            await NotifyStaffOfPriorityAsync(feedback, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (requireDraft && draft is null)
        {
            throw new DomainException(
                "The support agent completed its review but did not produce a reply draft. The feedback stays on record.");
        }

        return draft;
    }

    private static bool NeedsStaffAlert(FeedbackSupportAgentResponse response) =>
        response.ImmediateDashboardAlert
        || string.Equals(response.Priority, "High", StringComparison.OrdinalIgnoreCase);

    private async Task NotifyStaffOfPriorityAsync(Feedback feedback, CancellationToken cancellationToken)
    {
        var recipients = await _staffUsers.ListActiveAsync(cancellationToken);
        if (recipients.Count == 0)
        {
            return;
        }

        var who = feedback.IsAnonymous ? "An anonymous patient" : feedback.PatientNameSnapshot;
        var message = Truncate(
            $"{who} left feedback that needs review: {Truncate(feedback.Comment, 180)}",
            1000);
        foreach (var recipient in recipients)
        {
            await _notifications.AddAsync(new Notification
            {
                PatientId = feedback.PatientId,
                StaffUserId = recipient.Id,
                Title = "High-priority feedback",
                Message = message,
                Type = NotificationType.FeedbackAlert,
                IsRead = false
            }, cancellationToken);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static void ApplyAnalysis(Feedback feedback, FeedbackSupportAgentResponse response)
    {
        if (!string.IsNullOrWhiteSpace(response.Sentiment)
            && Enum.TryParse<FeedbackSentiment>(response.Sentiment, ignoreCase: true, out var sentiment))
        {
            feedback.Sentiment = sentiment;
        }

        if (!string.IsNullOrWhiteSpace(response.Category)
            && Enum.TryParse<FeedbackCategory>(response.Category, ignoreCase: true, out var category))
        {
            feedback.Category = category;
        }
    }

    private async Task PublishAsync(FeedbackReply reply, Guid staffUserId, CancellationToken cancellationToken)
    {
        var feedback = reply.Feedback ?? throw new DomainException("The draft is not linked to feedback.");
        reply.Status = FeedbackReplyStatus.Posted;
        reply.UserId = staffUserId;
        reply.UserRole = FeedbackReplyUserRole.Staff;
        await NotifyPatientAsync(feedback, reply.Reply, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyPatientAsync(Feedback feedback, string replyText, CancellationToken cancellationToken)
    {
        var message = replyText.Length <= 1000 ? replyText : replyText[..1000];
        await _notifications.AddAsync(new Notification
        {
            PatientId = feedback.PatientId,
            Title = "Reply to your feedback",
            Message = message,
            Type = NotificationType.FeedbackReply,
            IsRead = false
        }, cancellationToken);
    }
}
