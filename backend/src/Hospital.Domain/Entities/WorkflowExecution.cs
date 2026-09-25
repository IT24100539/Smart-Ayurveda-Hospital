using Hospital.Domain.Common;
using Hospital.Domain.Enums;

namespace Hospital.Domain.Entities;

/// <summary>
/// Durable record of one agent workflow. The id is the agent's workflow_id.
/// </summary>
public class WorkflowExecution : BaseEntity
{
    public string AgentName { get; set; } = string.Empty;
    public string ObjectiveText { get; set; } = string.Empty;
    public string PlanJson { get; set; } = "[]";
    public string CompletedStepsJson { get; set; } = "[]";
    public string ToolResultsJson { get; set; } = "[]";
    public string ValidationResultsJson { get; set; } = "[]";
    public string? ErrorsJson { get; set; }
    public WorkflowApprovalStatus ApprovalStatus { get; set; } = WorkflowApprovalStatus.Pending;
    public WorkflowFinalOutcome FinalOutcome { get; set; } = WorkflowFinalOutcome.InProgress;
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
