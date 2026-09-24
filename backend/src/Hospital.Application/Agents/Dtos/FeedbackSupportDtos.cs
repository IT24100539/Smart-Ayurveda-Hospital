using System.Text.Json.Serialization;

namespace Hospital.Application.Agents.Dtos;

/// <summary>
/// Body for POST /internal/agents/feedback-support. The agent returns analysis and a draft;
/// it does not publish a reply.
/// </summary>
public sealed record FeedbackSupportAgentRequest(
    [property: JsonPropertyName("feedback_id")] Guid FeedbackId,
    [property: JsonPropertyName("comment_text")] string CommentText,
    [property: JsonPropertyName("patient_id")] Guid PatientId);

public sealed record FeedbackSupportAgentResponse(
    [property: JsonPropertyName("sentiment")] string? Sentiment,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("priority")] string? Priority,
    [property: JsonPropertyName("similar_feedback_count")] int? SimilarFeedbackCount,
    [property: JsonPropertyName("suggested_reply")] string? SuggestedReply,
    [property: JsonPropertyName("draft_skipped")] bool DraftSkipped,
    [property: JsonPropertyName("workflow_id")] string WorkflowId,
    [property: JsonPropertyName("status")] string? Status = null,
    [property: JsonPropertyName("immediate_dashboard_alert")] bool ImmediateDashboardAlert = false);
