using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.UnitTests;

public sealed class ReactionServiceTests
{
    [Fact]
    public async Task React_SecondReactionFromSameUser_ReplacesTheFirst()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        feedback.Status = FeedbackStatus.Visible;

        await harness.Reactions.ReactAsync(feedback.Id, new ReactionRequest(FeedbackReactionType.Like), CancellationToken.None);
        var second = await harness.Reactions.ReactAsync(feedback.Id, new ReactionRequest(FeedbackReactionType.Dislike), CancellationToken.None);

        Assert.Equal(FeedbackReactionType.Dislike, second.ReactionType);
        var stored = Assert.Single(harness.ReactionStore.Items);
        Assert.Equal(FeedbackReactionType.Dislike, stored.ReactionType);
        Assert.Equal(second.Id, stored.Id);
    }

    [Fact]
    public async Task Remove_DeletesTheOnlyReaction()
    {
        var harness = new FeedbackHarness();
        var feedback = harness.SeedFeedback(FeedbackTestClock.Now);
        feedback.Status = FeedbackStatus.Visible;
        await harness.Reactions.ReactAsync(feedback.Id, new ReactionRequest(FeedbackReactionType.Like), CancellationToken.None);

        await harness.Reactions.RemoveAsync(feedback.Id, CancellationToken.None);

        Assert.Empty(harness.ReactionStore.Items);
    }
}
