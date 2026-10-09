using System.Text.Json.Serialization;

namespace Hospital.Application.Agents.Dtos;

/// <summary>One earlier line from the Charaka conversation.</summary>
public sealed record CharakaChatTurn(string Role, string Text);

/// <summary>Patient message for Charaka. History lets the reply follow the chat.</summary>
public sealed record AskCharakaRequest(
    string Question,
    IReadOnlyList<CharakaChatTurn>? History);

public sealed record AskCharakaResponse(
    string Answer,
    bool Refused,
    Guid WorkflowId);

/// <summary>Body for POST /internal/agents/charaka.</summary>
public sealed record CharakaAgentTurn(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("text")] string Text);

public sealed record CharakaAgentRequest(
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("history")] IReadOnlyList<CharakaAgentTurn>? History);

public sealed record CharakaAgentResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("refused")] bool Refused,
    [property: JsonPropertyName("workflow_id")] string? WorkflowId);
