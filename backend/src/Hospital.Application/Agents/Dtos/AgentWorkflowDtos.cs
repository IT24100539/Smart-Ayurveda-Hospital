namespace Hospital.Application.Agents.Dtos;

public sealed record StartAgentWorkflowRequest(
    string? Objective,
    IReadOnlyDictionary<string, string>? Context,
    string? ObjectiveText = null,
    string? PatientId = null)
{
    public StartAgentWorkflowRequest Normalized()
    {
        var objective = string.IsNullOrWhiteSpace(Objective) ? ObjectiveText : Objective;
        var context = Context is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(Context, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(PatientId)
            && !context.ContainsKey("patient_id")
            && !context.ContainsKey("patientId"))
        {
            context["patient_id"] = PatientId;
        }

        return new StartAgentWorkflowRequest(objective?.Trim(), context);
    }
}

public sealed record CoordinatorAgentResponse(
    Guid WorkflowId,
    string DelegatedTo,
    string Summary,
    string? ApprovalStatus,
    string? FinalOutcome);

public sealed record ApproveAgentWorkflowRequest(bool Approve, string? Reply, string? Decision = null);
