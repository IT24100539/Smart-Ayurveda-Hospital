using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Communication;

public static class FeedbackMapper
{
    public const string AnonymousPatientName = "Anonymous patient";

    public static FeedbackSummaryDto ToSummary(Feedback feedback) => new(
        feedback.Id,
        VisiblePatientId(feedback),
        VisiblePatientName(feedback),
        feedback.AppointmentId,
        feedback.TreatmentId,
        feedback.Rating,
        feedback.Comment,
        feedback.IsAnonymous,
        feedback.Sentiment,
        feedback.Category,
        feedback.Status,
        Count(feedback, FeedbackReactionType.Like),
        Count(feedback, FeedbackReactionType.Dislike),
        feedback.Replies.Count(x => x.Status == FeedbackReplyStatus.Posted),
        feedback.CreatedAt);

    public static PublicFeedbackDto ToPublic(Feedback feedback)
    {
        var summary = ToSummary(feedback);
        var replies = feedback.Replies
            .Where(x => x.Status == FeedbackReplyStatus.Posted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new PublicReplyDto(x.Id, x.UserRole, x.Reply, x.CreatedAt))
            .ToList();

        return new PublicFeedbackDto(
            summary.Id,
            summary.PatientId,
            summary.PatientName,
            summary.AppointmentId,
            summary.TreatmentId,
            summary.Rating,
            summary.Comment,
            summary.IsAnonymous,
            summary.Sentiment,
            summary.Category,
            summary.Status,
            summary.LikeCount,
            summary.DislikeCount,
            summary.PostedReplyCount,
            replies,
            summary.CreatedAt);
    }

    public static FeedbackDetailDto ToDetail(Feedback feedback, bool includeUnpostedReplies) => new(
        feedback.Id,
        VisiblePatientId(feedback),
        VisiblePatientName(feedback),
        feedback.AppointmentId,
        feedback.TreatmentId,
        feedback.Rating,
        feedback.Comment,
        feedback.IsAnonymous,
        feedback.Sentiment,
        feedback.Category,
        feedback.Status,
        feedback.ModeratedBy,
        feedback.ModeratedAt,
        Count(feedback, FeedbackReactionType.Like),
        Count(feedback, FeedbackReactionType.Dislike),
        feedback.Replies
            .Where(x => includeUnpostedReplies || x.Status == FeedbackReplyStatus.Posted)
            .OrderBy(x => x.CreatedAt)
            .Select(ToReply)
            .ToList(),
        feedback.CreatedAt,
        feedback.UpdatedAt);

    public static ReplyDto ToReply(FeedbackReply reply) => new(
        reply.Id,
        reply.FeedbackId,
        reply.UserId,
        reply.UserRole,
        reply.Reply,
        reply.IsAiGenerated,
        reply.Status,
        reply.CreatedAt);

    /// <summary>
    /// Anonymous comments keep <see cref="Feedback.PatientNameSnapshot"/> in the database,
    /// but that stored name is never copied onto a DTO.
    /// </summary>
    public static string VisiblePatientName(Feedback feedback) =>
        feedback.IsAnonymous ? AnonymousPatientName : feedback.PatientNameSnapshot;

    public static Guid? VisiblePatientId(Feedback feedback) =>
        feedback.IsAnonymous ? null : feedback.PatientId;

    private static int Count(Feedback feedback, FeedbackReactionType type) =>
        feedback.Reactions.Count(x => x.ReactionType == type);
}
