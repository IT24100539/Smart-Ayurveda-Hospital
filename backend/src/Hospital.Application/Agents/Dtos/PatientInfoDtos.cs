namespace Hospital.Application.Agents.Dtos;

public sealed record AskPatientInfoRequest(string Question);

public sealed record AskPatientInfoResponse(
    string Answer,
    bool Refused,
    Guid WorkflowId);

public sealed record PatientInfoAgentRequest(
    Guid PatientId,
    string Question);

public sealed record PatientInfoAgentResponse(
    string Answer,
    bool Refused,
    string WorkflowId);
