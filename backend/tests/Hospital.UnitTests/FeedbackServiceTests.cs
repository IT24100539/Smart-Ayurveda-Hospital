using System.Text.Json;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class FeedbackServiceTests
{
    private static readonly DateTimeOffset Now = FeedbackTestClock.Now;

    [Fact]
    public async Task Create_WhenAppointmentIsNotCompleted_Throws()
    {
        var harness = new FeedbackHarness();
        harness.Appointments.Appointment = FeedbackHarness.Visit(harness.Patient.Id, AppointmentStatus.Approved);

        var act = () => harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            harness.Appointments.Appointment.Id,
            null,
            4,
            "The visit felt rushed.",
            false), CancellationToken.None);

        var error = await Assert.ThrowsAsync<DomainException>(act);
        Assert.Contains("completed", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(harness.FeedbackStore.Items);
    }

    [Fact]
    public async Task Create_WhenAppointmentBelongsToSomeoneElse_Throws()
    {
        var harness = new FeedbackHarness();
        harness.Appointments.Appointment = FeedbackHarness.Visit(Guid.NewGuid(), AppointmentStatus.Completed);

        await Assert.ThrowsAsync<DomainException>(() => harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            harness.Appointments.Appointment.Id,
            null,
            5,
            "Helpful nadi pariksha.",
            false), CancellationToken.None));
    }

    [Fact]
    public async Task Create_TreatmentOnly_SkipsAppointmentCheck()
    {
        var harness = new FeedbackHarness();
        var treatmentId = Guid.NewGuid();
        harness.Treatments.Ids.Add(treatmentId);

        var created = await harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            null,
            treatmentId,
            5,
            "Shirodhara was calming.",
            false), CancellationToken.None);

        Assert.Equal(0, harness.Appointments.GetCalls);
        Assert.Equal(treatmentId, created.TreatmentId);
        Assert.Equal(FeedbackStatus.PendingModeration, created.Status);
    }

    [Fact]
    public async Task Create_Anonymous_StoresSnapshotButDtoHidesIt()
    {
        var harness = new FeedbackHarness();
        harness.Appointments.Appointment = FeedbackHarness.Visit(harness.Patient.Id, AppointmentStatus.Completed);

        var created = await harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            harness.Appointments.Appointment.Id,
            null,
            2,
            "The waiting room was noisy.",
            true), CancellationToken.None);

        Assert.Equal("Secret Name", harness.FeedbackStore.Items.Single().PatientNameSnapshot);
        Assert.Equal("Anonymous patient", created.PatientName);
        Assert.Null(created.PatientId);
        var json = JsonSerializer.Serialize(created);
        Assert.DoesNotContain("Secret Name", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Edit_After24Hours_Throws()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(Now.AddHours(-24).AddMinutes(-1));

        var error = await Assert.ThrowsAsync<DomainException>(() => harness.Feedback.EditAsync(
            feedback.Id,
            new UpdateFeedbackRequest(null, "Changed my mind.", null, false),
            CancellationToken.None));

        Assert.Contains("24", error.Message, StringComparison.Ordinal);
        Assert.Equal("Original comment.", feedback.Comment);
    }

    [Fact]
    public async Task Edit_Within24Hours_UpdatesAndReturnsToModeration()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(Now.AddHours(-2));
        feedback.Status = FeedbackStatus.Visible;

        var updated = await harness.Feedback.EditAsync(
            feedback.Id,
            new UpdateFeedbackRequest(3, "The abhyanga oil was too warm.", null, false),
            CancellationToken.None);

        Assert.Equal(3, updated.Rating);
        Assert.Equal(FeedbackStatus.PendingModeration, updated.Status);
        Assert.Null(feedback.ModeratedById);
    }

    [Fact]
    public async Task Edit_WithdrawsOwnFeedback()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(Now.AddMinutes(-30));

        var updated = await harness.Feedback.EditAsync(
            feedback.Id,
            new UpdateFeedbackRequest(null, null, null, true),
            CancellationToken.None);

        Assert.Equal(FeedbackStatus.Hidden, updated.Status);
    }

    [Fact]
    public async Task Edit_OtherPatientsFeedback_Throws()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(Now);
        feedback.PatientId = Guid.NewGuid();

        await Assert.ThrowsAsync<ForbiddenException>(() => harness.Feedback.EditAsync(
            feedback.Id,
            new UpdateFeedbackRequest(null, "Not mine.", null, false),
            CancellationToken.None));
    }

    [Fact]
    public async Task PublicFeed_OmitsHiddenRows_AndAnonymousNames()
    {
        var harness = new FeedbackHarness();
        var visible = harness.SeedFeedback(Now);
        visible.Status = FeedbackStatus.Visible;
        visible.IsAnonymous = true;
        visible.PatientNameSnapshot = "Secret Name";
        var hidden = harness.SeedFeedback(Now);
        hidden.Status = FeedbackStatus.Hidden;
        hidden.Comment = "Should stay off the feed.";

        var feed = await harness.Feedback.GetPublicFeedAsync(CancellationToken.None);

        var item = Assert.Single(feed);
        Assert.Equal("Anonymous patient", item.PatientName);
        Assert.Null(item.PatientId);
        Assert.DoesNotContain("Secret Name", JsonSerializer.Serialize(feed), StringComparison.Ordinal);
        Assert.DoesNotContain("Should stay off the feed.", feed.Select(x => x.Comment));
    }

    [Fact]
    public async Task PublicFeed_IncludesPostedReplies_AndOmitsDraftsAndAccountIds()
    {
        var harness = new FeedbackHarness();
        var visible = harness.SeedFeedback(Now);
        visible.Status = FeedbackStatus.Visible;
        visible.IsAnonymous = true;
        visible.PatientNameSnapshot = "Secret Name";
        var accountId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        visible.Replies.Add(new FeedbackReply
        {
            FeedbackId = visible.Id,
            UserId = accountId,
            UserRole = FeedbackReplyUserRole.Staff,
            Reply = "Still a draft.",
            Status = FeedbackReplyStatus.Draft,
            CreatedAt = Now
        });
        visible.Replies.Add(new FeedbackReply
        {
            FeedbackId = visible.Id,
            UserId = accountId,
            UserRole = FeedbackReplyUserRole.Patient,
            Reply = "The abhyanga helped my vata.",
            Status = FeedbackReplyStatus.Posted,
            CreatedAt = Now.AddMinutes(2)
        });
        visible.Replies.Add(new FeedbackReply
        {
            FeedbackId = visible.Id,
            UserRole = FeedbackReplyUserRole.Staff,
            Reply = "We are glad the session helped.",
            Status = FeedbackReplyStatus.Posted,
            CreatedAt = Now.AddMinutes(1)
        });

        var feed = await harness.Feedback.GetPublicFeedAsync(CancellationToken.None);

        var item = Assert.Single(feed);
        Assert.Equal(2, item.PostedReplyCount);
        Assert.Equal(
            new[] { FeedbackReplyUserRole.Staff, FeedbackReplyUserRole.Patient },
            item.Replies.Select(x => x.UserRole).ToArray());
        Assert.Equal("We are glad the session helped.", item.Replies[0].Reply);
        var json = JsonSerializer.Serialize(feed);
        Assert.DoesNotContain("Secret Name", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Still a draft.", json, StringComparison.Ordinal);
        Assert.DoesNotContain(accountId.ToString(), json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Moderate_RecordsStaffAndStatus()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(Now);

        var moderated = await harness.Feedback.ModerateAsync(
            feedback.Id,
            FeedbackModerationAction.Show,
            CancellationToken.None);

        Assert.Equal(FeedbackStatus.Visible, moderated.Status);
        Assert.Equal(harness.Staff.Id, moderated.ModeratedBy);
        Assert.Equal(Now, moderated.ModeratedAt);
    }

    [Fact]
    public async Task GetForStaff_IncludesDraftRepliesAndHidesAnonymousName()
    {
        var harness = new FeedbackHarness(actAsStaff: true);
        var feedback = harness.SeedFeedback(Now);
        feedback.IsAnonymous = true;
        feedback.PatientNameSnapshot = "Secret Name";
        feedback.ModeratedById = harness.Staff.Id;
        feedback.ModeratedAt = Now;
        feedback.Replies.Add(new FeedbackReply
        {
            FeedbackId = feedback.Id,
            Reply = "Namaste. We will look into the wait.",
            IsAiGenerated = true,
            Status = FeedbackReplyStatus.Draft,
            UserRole = FeedbackReplyUserRole.Staff,
            CreatedAt = Now
        });

        var detail = await harness.Feedback.GetForStaffAsync(feedback.Id, CancellationToken.None);

        Assert.Equal("Anonymous patient", detail.PatientName);
        Assert.Null(detail.PatientId);
        Assert.Equal(harness.Staff.Id, detail.ModeratedBy);
        Assert.Equal(Now, detail.ModeratedAt);
        var reply = Assert.Single(detail.Replies);
        Assert.True(reply.IsAiGenerated);
        Assert.Equal(FeedbackReplyStatus.Draft, reply.Status);
        Assert.DoesNotContain("Secret Name", JsonSerializer.Serialize(detail), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_WhenAgentSucceeds_StoresAnalysisAndUnpublishedDraft()
    {
        var harness = new FeedbackHarness();
        harness.Appointments.Appointment = FeedbackHarness.Visit(harness.Patient.Id, AppointmentStatus.Completed);
        harness.Agents.Sentiment = "Positive";
        harness.Agents.Category = "TreatmentQuality";

        var created = await harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            harness.Appointments.Appointment.Id,
            null,
            5,
            "The nadi pariksha was clear and unhurried.",
            false), CancellationToken.None);

        Assert.Equal(FeedbackStatus.PendingModeration, created.Status);
        Assert.Equal(FeedbackSentiment.Positive, harness.FeedbackStore.Items.Single().Sentiment);
        Assert.Equal(FeedbackCategory.TreatmentQuality, harness.FeedbackStore.Items.Single().Category);
        var draft = Assert.Single(harness.ReplyStore.Items);
        Assert.Equal(FeedbackReplyStatus.Draft, draft.Status);
        Assert.True(draft.IsAiGenerated);
        Assert.Empty(harness.NotificationStore.Items);
        Assert.DoesNotContain(created.Replies, reply => reply.Status == FeedbackReplyStatus.Posted);
    }

    [Fact]
    public async Task Create_WhenAgentFails_StillStoresFeedback()
    {
        var harness = new FeedbackHarness();
        harness.Appointments.Appointment = FeedbackHarness.Visit(harness.Patient.Id, AppointmentStatus.Completed);
        harness.Agents.Failure = new InvalidOperationException("agent timed out");

        var created = await harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            harness.Appointments.Appointment.Id,
            null,
            2,
            "The waiting room was crowded before shirodhara.",
            false), CancellationToken.None);

        Assert.Equal(FeedbackStatus.PendingModeration, created.Status);
        Assert.Null(harness.FeedbackStore.Items.Single().Sentiment);
        Assert.Empty(harness.ReplyStore.Items);
    }

    [Fact]
    public async Task Create_WhenAgentFlagsHighPriority_AlertsStaffOnly()
    {
        var harness = new FeedbackHarness();
        harness.Staff.Role = StaffRole.Admin;
        harness.Appointments.Appointment = FeedbackHarness.Visit(harness.Patient.Id, AppointmentStatus.Completed);
        harness.Agents.Sentiment = "Negative";
        harness.Agents.Category = "StaffService";
        harness.Agents.Priority = "High";
        harness.Agents.ImmediateDashboardAlert = true;

        await harness.Feedback.CreateAsync(new CreateFeedbackRequest(
            harness.Appointments.Appointment.Id,
            null,
            1,
            "The therapist dismissed my vata symptoms.",
            true), CancellationToken.None);

        var notice = Assert.Single(harness.NotificationStore.Items);
        Assert.Equal(NotificationType.FeedbackAlert, notice.Type);
        Assert.Equal(harness.Staff.Id, notice.StaffUserId);
        Assert.Equal(harness.Patient.Id, notice.PatientId);
        var inbox = await harness.Notifications.GetForPatient(harness.Patient.Id, CancellationToken.None);
        Assert.Empty(inbox);
    }
}
