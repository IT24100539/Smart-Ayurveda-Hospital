using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class ComplaintServiceTests
{
    private static readonly DateTimeOffset Now = FeedbackTestClock.Now;

    [Fact]
    public async Task GetOverdueComplaints_ReturnsOpenOlderThanFiveDays()
    {
        var harness = new FeedbackHarness();
        harness.ComplaintStore.Items.Add(FeedbackHarness.OpenComplaint(harness.Patient, Now.AddDays(-5).AddMinutes(-1), ComplaintStatus.Open));
        harness.ComplaintStore.Items.Add(FeedbackHarness.OpenComplaint(harness.Patient, Now.AddDays(-1), ComplaintStatus.Open));
        harness.ComplaintStore.Items.Add(FeedbackHarness.OpenComplaint(harness.Patient, Now.AddDays(-8), ComplaintStatus.Escalated));
        harness.ComplaintStore.Items.Add(FeedbackHarness.OpenComplaint(harness.Patient, Now.AddDays(-5), ComplaintStatus.Open));

        var overdue = await harness.Complaints.GetOverdueComplaintsAsync(CancellationToken.None);

        var item = Assert.Single(overdue);
        Assert.True(item.IsOverdue);
        Assert.Equal(ComplaintStatus.Open, item.Status);
        Assert.Equal(Now.AddDays(-5).AddMinutes(-1), item.CreatedAt);
    }

    [Fact]
    public async Task UpdateStatus_Escalated_SetsPriorityAndNotifies()
    {
        var harness = new FeedbackHarness();
        var complaint = FeedbackHarness.OpenComplaint(harness.Patient, Now.AddDays(-2), ComplaintStatus.Open);
        harness.ComplaintStore.Items.Add(complaint);

        var updated = await harness.Complaints.UpdateStatusAsync(
            complaint.Id,
            new ComplaintStatusUpdateRequest(ComplaintStatus.Escalated, null),
            CancellationToken.None);

        Assert.Equal(ComplaintStatus.Escalated, updated.Status);
        Assert.Equal(ComplaintPriority.High, updated.Priority);
        Assert.Equal(Now, updated.EscalatedAt);
        Assert.Equal(harness.Staff.Id, updated.AssignedTo);
        Assert.Equal(NotificationType.ComplaintEscalated, Assert.Single(harness.NotificationStore.Items).Type);
    }

    [Fact]
    public async Task ListMine_ReturnsOnlyTheSignedInPatientsComplaints()
    {
        var harness = new FeedbackHarness();
        var mine = FeedbackHarness.OpenComplaint(harness.Patient, Now, ComplaintStatus.InProgress);
        var someoneElse = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "Other",
            LastName = "Patient"
        };
        var theirs = FeedbackHarness.OpenComplaint(someoneElse, Now.AddHours(-1), ComplaintStatus.Open);
        harness.ComplaintStore.Items.Add(mine);
        harness.ComplaintStore.Items.Add(theirs);

        var list = await harness.Complaints.ListMineAsync(CancellationToken.None);

        var item = Assert.Single(list);
        Assert.Equal(mine.Id, item.Id);
        Assert.Equal(harness.Patient.Id, item.PatientId);
        Assert.Equal(ComplaintStatus.InProgress, item.Status);
    }

    [Fact]
    public async Task Get_OtherPatientsComplaint_IsForbidden()
    {
        var harness = new FeedbackHarness();
        var theirs = FeedbackHarness.OpenComplaint(harness.Patient, Now, ComplaintStatus.Open);
        theirs.PatientId = Guid.NewGuid();
        harness.ComplaintStore.Items.Add(theirs);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            harness.Complaints.GetAsync(theirs.Id, CancellationToken.None));
    }
}

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task MarkRead_OnlyOwnNotification()
    {
        var harness = new FeedbackHarness();
        var mine = new Notification
        {
            PatientId = harness.Patient.Id,
            Title = "Reply",
            Message = "A reply was posted.",
            Type = NotificationType.FeedbackReply
        };
        var other = new Notification
        {
            PatientId = Guid.NewGuid(),
            Title = "Other",
            Message = "Not yours.",
            Type = NotificationType.General
        };
        harness.NotificationStore.Items.Add(mine);
        harness.NotificationStore.Items.Add(other);

        var read = await harness.Notifications.MarkReadAsync(mine.Id, CancellationToken.None);
        Assert.True(read.IsRead);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            harness.Notifications.MarkReadAsync(other.Id, CancellationToken.None));
    }

    [Fact]
    public async Task MarkAllRead_UpdatesOnlyThePatientInbox()
    {
        var harness = new FeedbackHarness();
        harness.NotificationStore.Items.Add(new Notification
        {
            PatientId = harness.Patient.Id,
            Title = "Reply",
            Message = "A reply was posted.",
            Type = NotificationType.FeedbackReply
        });
        harness.NotificationStore.Items.Add(new Notification
        {
            PatientId = harness.Patient.Id,
            StaffUserId = harness.Staff.Id,
            Title = "High-priority feedback",
            Message = "Staff only.",
            Type = NotificationType.FeedbackAlert
        });

        var result = await harness.Notifications.MarkAllReadAsync(CancellationToken.None);

        Assert.Equal(1, result.Updated);
        Assert.True(harness.NotificationStore.Items[0].IsRead);
        Assert.False(harness.NotificationStore.Items[1].IsRead);
    }
}
