using System.Text.Json.Serialization;

namespace Hospital.Application.Agents.Dtos;

/// <summary>Patient question for the treatment-info agent. The patient app sends this to Hospital.Api.</summary>
public sealed record AskTreatmentInfoRequest(string Question);

/// <summary>Answer grounded in the treatment catalogue, or a refusal of medical advice.</summary>
public sealed record AskTreatmentInfoResponse(
    string Answer,
    IReadOnlyList<Guid> MatchedTreatmentIds,
    bool Refused,
    Guid WorkflowId);

/// <summary>Body for POST /internal/agents/treatment-info.</summary>
public sealed record TreatmentInfoAgentRequest(
    [property: JsonPropertyName("question")] string Question);

public sealed record TreatmentInfoAgentResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("matched_treatment_ids")] IReadOnlyList<string>? MatchedTreatmentIds,
    [property: JsonPropertyName("refused")] bool Refused,
    [property: JsonPropertyName("workflow_id")] string? WorkflowId);
