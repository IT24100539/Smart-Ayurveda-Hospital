using System.Text.Json;
using Hospital.Domain.Enums;

namespace Hospital.Application.Workflows.Dtos;

public sealed class UpsertWorkflowExecutionRequest
{
    public Guid Id { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string ObjectiveText { get; init; } = string.Empty;
    public JsonElement PlanJson { get; init; }
    public JsonElement CompletedStepsJson { get; init; }
    public JsonElement ToolResultsJson { get; init; }
    public JsonElement ValidationResultsJson { get; init; }
    public JsonElement? ErrorsJson { get; init; }
    public WorkflowApprovalStatus ApprovalStatus { get; init; } = WorkflowApprovalStatus.Pending;
    public WorkflowFinalOutcome FinalOutcome { get; init; } = WorkflowFinalOutcome.InProgress;
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
}

public sealed class UpdateWorkflowExecutionRequest
{
    public string AgentName { get; init; } = string.Empty;
    public string ObjectiveText { get; init; } = string.Empty;
    public JsonElement PlanJson { get; init; }
    public JsonElement CompletedStepsJson { get; init; }
    public JsonElement ToolResultsJson { get; init; }
    public JsonElement ValidationResultsJson { get; init; }
    public JsonElement? ErrorsJson { get; init; }
    public WorkflowApprovalStatus ApprovalStatus { get; init; } = WorkflowApprovalStatus.Pending;
    public WorkflowFinalOutcome FinalOutcome { get; init; } = WorkflowFinalOutcome.InProgress;
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
}

public sealed record WorkflowExecutionSearchQuery(
    string? AgentName,
    WorkflowApprovalStatus? ApprovalStatus,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Search,
    string? Sort,
    int Page = 1,
    int PageSize = 20);

public sealed class WorkflowExecutionDto
{
    public Guid Id { get; init; }
    public string AgentName { get; init; } = string.Empty;
    public string ObjectiveText { get; init; } = string.Empty;
    public JsonElement Plan { get; init; }
    public JsonElement CompletedSteps { get; init; }
    public JsonElement ToolResults { get; init; }
    public JsonElement ValidationResults { get; init; }
    public JsonElement? Errors { get; init; }
    public WorkflowApprovalStatus ApprovalStatus { get; init; }
    public WorkflowFinalOutcome FinalOutcome { get; init; }
    public string? RelatedEntityType { get; init; }
    public Guid? RelatedEntityId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
