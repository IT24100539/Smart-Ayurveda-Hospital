using Hospital.Application.Communication;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.UnitTests;

public sealed class InternalFeedbackServiceTests
{
    [Fact]
    public async Task Get_ReturnsCommentRatingAndPatientContext()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        feedback.Patient = harness.Patient;
        feedback.Patient.Uhid = "SAH-2026-00041";
        feedback.Patient.Prakriti = DoshaType.Vata;
        feedback.Patient.Vikriti = DoshaType.Pitta;
        feedback.Rating = 5;
        feedback.Comment = "Abhyanga eased the vata stiffness.";
        feedback.IsAnonymous = true;

        var service = new InternalFeedbackService(harness.FeedbackStore, harness.Clock);
        var context = await service.GetAsync(feedback.Id, CancellationToken.None);

        Assert.NotNull(context);
        Assert.Equal(feedback.Comment, context!.Comment);
        Assert.Equal(5, context.Rating);
        Assert.Equal(feedback.PatientId, context.PatientId);
        Assert.Equal("SAH-2026-00041", context.Uhid);
        Assert.Equal("Vata", context.Prakriti);
        Assert.Equal("Pitta", context.Vikriti);
        Assert.True(context.IsAnonymous);
    }

    [Fact]
    public async Task CountSimilar_CountsOtherRecentPatientsInTheSameCategory()
    {
        var harness = new FeedbackHarness();
        var mine = harness.SeedFeedback(FeedbackTestClock.Now);
        mine.Category = FeedbackCategory.WaitingTime;

        var other = new Patient { Id = Guid.NewGuid(), FirstName = "Arjun", LastName = "Rao" };
        harness.FeedbackStore.Items.Add(new Feedback
        {
            PatientId = other.Id,
            PatientNameSnapshot = "Arjun Rao",
            Rating = 2,
            Comment = "The nadi pariksha slot ran late.",
            Category = FeedbackCategory.WaitingTime,
            CreatedAt = FeedbackTestClock.Now.AddDays(-2)
        });
        harness.FeedbackStore.Items.Add(new Feedback
        {
            PatientId = other.Id,
            PatientNameSnapshot = "Arjun Rao",
            Rating = 3,
            Comment = "Shirodhara room was fine.",
            Category = FeedbackCategory.FacilityIssue,
            CreatedAt = FeedbackTestClock.Now.AddDays(-1)
        });
        harness.FeedbackStore.Items.Add(new Feedback
        {
            PatientId = Guid.NewGuid(),
            PatientNameSnapshot = "Old note",
            Rating = 2,
            Comment = "A wait from last season.",
            Category = FeedbackCategory.WaitingTime,
            CreatedAt = FeedbackTestClock.Now.AddDays(-40)
        });

        var service = new InternalFeedbackService(harness.FeedbackStore, harness.Clock);
        var count = await service.CountSimilarAsync(
            FeedbackCategory.WaitingTime,
            mine.PatientId,
            CancellationToken.None);

        Assert.Equal(1, count);
    }
}
