namespace Hospital.Application.Agents.Dtos;

public sealed record AgentInvokeRequest(
    string Agent,
    string Prompt,
    IReadOnlyDictionary<string, string>? Context);

public sealed record AgentInvokeResponse(
    string Agent,
    string Reply,
    IReadOnlyDictionary<string, string> Metadata);
