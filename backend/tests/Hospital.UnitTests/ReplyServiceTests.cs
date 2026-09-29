using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hospital.Application.Agents.Dtos;
using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class ReplyServiceTests
{
    [Fact]
    public async Task RequestAiDraft_StaysDraft_AndDoesNotNotify()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        harness.Agents.Reply = "This reply is ready to publish.";

        var draft = await harness.Replies.RequestAiDraftAsync(feedback.Id, CancellationToken.None);

        Assert.Equal(FeedbackReplyStatus.Draft, draft.Status);
        Assert.True(draft.IsAiGenerated);
        Assert.Null(draft.UserId);
        Assert.Empty(harness.NotificationStore.Items);
        Assert.Equal(1, harness.Agents.Calls);
        Assert.Equal(FeedbackSentiment.Positive, feedback.Sentiment);
        Assert.Equal(FeedbackCategory.TreatmentQuality, feedback.Category);
    }

    [Fact]
    public async Task RequestAiDraft_WhenDraftSkipped_KeepsFeedbackAndDoesNotStoreAReply()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        harness.Agents.DraftSkipped = true;
        harness.Agents.Sentiment = "Negative";
        harness.Agents.Category = "StaffService";

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            harness.Replies.RequestAiDraftAsync(feedback.Id, CancellationToken.None));

        Assert.Contains("did not produce a reply draft", error.Message);
        Assert.Empty(harness.ReplyStore.Items);
        Assert.Empty(harness.NotificationStore.Items);
        Assert.Equal(FeedbackSentiment.Negative, feedback.Sentiment);
        Assert.Equal(FeedbackCategory.StaffService, feedback.Category);
    }

    [Fact]
    public async Task DecideDraft_Approve_PostsAndNotifiesPatient()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        var draft = Draft(feedback, "Namaste. We are sorry the slot ran late.");
        harness.ReplyStore.Items.Add(draft);

        var approved = await harness.Replies.DecideDraftAsync(
            draft.Id,
            new ReplyDecisionRequest(ReplyDecision.Approve, null),
            CancellationToken.None);

        Assert.Equal(FeedbackReplyStatus.Posted, approved.Status);
        Assert.True(approved.IsAiGenerated);
        Assert.Equal(harness.StaffUser.Id, approved.UserId);
        var notice = Assert.Single(harness.NotificationStore.Items);
        Assert.Equal(feedback.PatientId, notice.PatientId);
        Assert.Equal(NotificationType.FeedbackReply, notice.Type);
        Assert.Equal(draft.Reply, notice.Message);
    }

    [Fact]
    public async Task DecideDraft_Reject_LeavesNothingPostedAndDoesNotNotify()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        var draft = Draft(feedback, "Namaste. We are sorry the slot ran late.");
        harness.ReplyStore.Items.Add(draft);

        var rejected = await harness.Replies.DecideDraftAsync(
            draft.Id,
            new ReplyDecisionRequest(ReplyDecision.Reject, null),
            CancellationToken.None);

        Assert.Equal(FeedbackReplyStatus.Rejected, rejected.Status);
        Assert.NotEqual(FeedbackReplyStatus.Posted, draft.Status);
        Assert.Empty(harness.NotificationStore.Items);
    }

    [Fact]
    public async Task DecideDraft_EditPublishesStaffText()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        var draft = Draft(feedback, "Agent wording.");
        harness.ReplyStore.Items.Add(draft);

        var posted = await harness.Replies.DecideDraftAsync(
            draft.Id,
            new ReplyDecisionRequest(ReplyDecision.Edit, "We have moved your nadi pariksha follow-up forward."),
            CancellationToken.None);

        Assert.Equal(FeedbackReplyStatus.Posted, posted.Status);
        Assert.Equal("We have moved your nadi pariksha follow-up forward.", posted.Reply);
        Assert.Single(harness.NotificationStore.Items);
    }

    [Fact]
    public async Task DecideDraft_Save_KeepsTheDraftUnpublished()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        var draft = Draft(feedback, "Agent wording.");
        harness.ReplyStore.Items.Add(draft);

        var saved = await harness.Replies.DecideDraftAsync(
            draft.Id,
            new ReplyDecisionRequest(ReplyDecision.Save, "We will review the morning nadi pariksha queue."),
            CancellationToken.None);

        Assert.Equal(FeedbackReplyStatus.Draft, saved.Status);
        Assert.Equal("We will review the morning nadi pariksha queue.", saved.Reply);
        Assert.Empty(harness.NotificationStore.Items);
    }

    [Fact]
    public async Task CreateManualReply_IsPostedImmediately_AndNotifiesThePatient()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        feedback.Status = FeedbackStatus.PendingModeration;

        var posted = await harness.Replies.CreateManualReplyAsync(
            feedback.Id,
            new CreateReplyRequest("Namaste. A vaidya will review the wait for abhyanga."),
            CancellationToken.None);

        Assert.Equal(FeedbackReplyStatus.Posted, posted.Status);
        Assert.False(posted.IsAiGenerated);
        Assert.Equal(FeedbackReplyUserRole.Staff, posted.UserRole);
        var notice = Assert.Single(harness.NotificationStore.Items);
        Assert.Equal(feedback.PatientId, notice.PatientId);
        Assert.Null(notice.StaffUserId);
        Assert.Equal(NotificationType.FeedbackReply, notice.Type);
        Assert.Equal(posted.Reply, notice.Message);
    }

    [Fact]
    public async Task CreateManualReply_WhenWithdrawn_DoesNotPost()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        feedback.Status = FeedbackStatus.Withdrawn;

        var error = await Assert.ThrowsAsync<DomainException>(() => harness.Replies.CreateManualReplyAsync(
            feedback.Id,
            new CreateReplyRequest("This should not be stored."),
            CancellationToken.None));

        Assert.Contains("withdrawn", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(harness.ReplyStore.Items);
        Assert.Empty(harness.NotificationStore.Items);
    }

    [Fact]
    public async Task RequestAiDraft_WhenAgentThrows_LeavesFeedbackAndExplains()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        harness.Agents.Failure = new HttpRequestException("connection refused", null, HttpStatusCode.BadGateway);

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            harness.Replies.RequestAiDraftAsync(feedback.Id, CancellationToken.None));

        Assert.Equal(ReplyService.AiUnavailableMessage, error.Message);
        Assert.Null(feedback.Sentiment);
        Assert.Empty(harness.ReplyStore.Items);
        var logged = Assert.Single(harness.AgentLog.Entries);
        Assert.Contains("HttpRequestException", logged.Message, StringComparison.Ordinal);
        Assert.Contains("502", logged.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("dev-internal-agent-secret", logged.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("X-Internal-Secret", logged.Exception?.ToString() ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestAiDraft_WhenRulesCouldNotDraft_KeepsAnalysisAndSaysTheServiceIsUnavailable()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        harness.Agents.DraftSkipped = true;
        harness.Agents.ClassifiedBy = "rules";
        harness.Agents.Sentiment = "Negative";
        harness.Agents.Category = "WaitingTime";

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            harness.Replies.RequestAiDraftAsync(feedback.Id, CancellationToken.None));

        Assert.Equal(ReplyService.AiUnavailableMessage, error.Message);
        Assert.Equal(FeedbackSentiment.Negative, feedback.Sentiment);
        Assert.Equal(FeedbackCategory.WaitingTime, feedback.Category);
        Assert.Empty(harness.ReplyStore.Items);
    }

    [Fact]
    public void StaffDto_IncludesSentimentAndCategoryAsStrings()
    {
        var feedback = new Feedback
        {
            PatientNameSnapshot = "Meera Nair",
            Rating = 2,
            Comment = "The queue for abhyanga ran long.",
            Sentiment = FeedbackSentiment.Negative,
            Category = FeedbackCategory.WaitingTime,
            Status = FeedbackStatus.PendingModeration
        };

        var summary = FeedbackMapper.ToSummary(feedback);
        var detail = FeedbackMapper.ToDetail(feedback, includeUnpostedReplies: true);
        Assert.Equal(FeedbackSentiment.Negative, summary.Sentiment);
        Assert.Equal(FeedbackCategory.WaitingTime, summary.Category);
        Assert.Equal(FeedbackSentiment.Negative, detail.Sentiment);
        Assert.Equal(FeedbackCategory.WaitingTime, detail.Category);

        var json = JsonSerializer.Serialize(detail, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        });
        Assert.Contains("\"sentiment\":\"Negative\"", json, StringComparison.Ordinal);
        Assert.Contains("\"category\":\"WaitingTime\"", json, StringComparison.Ordinal);

        var agent = JsonSerializer.Deserialize<FeedbackSupportAgentResponse>("""
            {
              "sentiment": "Negative",
              "category": "WaitingTime",
              "priority": "Normal",
              "similar_feedback_count": 1,
              "suggested_reply": null,
              "draft_skipped": true,
              "workflow_id": "wf",
              "classified_by": "rules"
            }
            """);
        Assert.NotNull(agent);
        Assert.Equal("Negative", agent.Sentiment);
        Assert.Equal("WaitingTime", agent.Category);
        Assert.Equal("rules", agent.ClassifiedBy);
    }

    private static FeedbackReply Draft(Feedback feedback, string text) => new()
    {
        FeedbackId = feedback.Id,
        Feedback = feedback,
        UserRole = FeedbackReplyUserRole.Staff,
        Reply = text,
        IsAiGenerated = true,
        Status = FeedbackReplyStatus.Draft
    };
}
