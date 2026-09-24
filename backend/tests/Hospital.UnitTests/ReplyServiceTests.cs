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
    public async Task RequestAiDraft_WhenAgentThrows_LeavesFeedbackAndExplains()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        harness.Agents.Failure = new HttpRequestException("connection refused");

        var error = await Assert.ThrowsAsync<DomainException>(() =>
            harness.Replies.RequestAiDraftAsync(feedback.Id, CancellationToken.None));

        Assert.Contains("unavailable", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(feedback.Sentiment);
        Assert.Empty(harness.ReplyStore.Items);
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
