using Hospital.Application.Abstractions;
using Hospital.Application.Agents.Dtos;
using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Hospital.Application.Wards;
using Hospital.Application.Workflows;
using Hospital.Application.Workflows.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Agents;

public sealed class AgentWorkflowService : IAgentWorkflowService
{
    private readonly IAgentClient _agents;
    private readonly IWorkflowExecutionRepository _executions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActorContext _actors;
    private readonly IWardService _wards;
    private readonly IReplyService _replies;
    private readonly IFeedbackReplyRepository _replyRecords;

    public AgentWorkflowService(
        IAgentClient agents,
        IWorkflowExecutionRepository executions,
        IUnitOfWork unitOfWork,
        IActorContext actors,
        IWardService wards,
        IReplyService replies,
        IFeedbackReplyRepository replyRecords)
    {
        _agents = agents;
        _executions = executions;
        _unitOfWork = unitOfWork;
        _actors = actors;
        _wards = wards;
        _replies = replies;
        _replyRecords = replyRecords;
    }

    public Task<CoordinatorAgentResponse> StartAsync(StartAgentWorkflowRequest request, CancellationToken cancellationToken) =>
        _agents.CoordinateAsync(request.Normalized(), cancellationToken);

    public async Task<AskTreatmentInfoResponse> AskTreatmentAsync(
        AskTreatmentInfoRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        if (user.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only a patient can perform this action.");
        }
        var response = await _agents.AskTreatmentInfoAsync(
            new TreatmentInfoAgentRequest(request.Question.Trim()),
            cancellationToken);

        var matched = new List<Guid>();
        foreach (var id in response.MatchedTreatmentIds ?? [])
        {
            if (Guid.TryParse(id, out var parsed))
            {
                matched.Add(parsed);
            }
        }

        Guid.TryParse(response.WorkflowId, out var workflowId);
        return new AskTreatmentInfoResponse(
            response.Answer ?? "",
            matched,
            response.Refused,
            workflowId);
    }

    public async Task<AskPatientInfoResponse> AskPatientInfoAsync(
        AskPatientInfoRequest request,
        CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var response = await _agents.AskPatientInfoAsync(
            new PatientInfoAgentRequest(patient.Id, request.Question.Trim()),
            cancellationToken);

        Guid.TryParse(response.WorkflowId, out var workflowId);
        return new AskPatientInfoResponse(
            response.Answer ?? "",
            response.Refused,
            workflowId);
    }

    public async Task<AskCharakaResponse> AskCharakaAsync(
        AskCharakaRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        if (user.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only a patient can perform this action.");
        }

        var history = (request.History ?? [])
            .Select(turn => new CharakaAgentTurn(turn.Role.Trim(), turn.Text.Trim()))
            .ToList();
        var response = await _agents.AskCharakaAsync(
            new CharakaAgentRequest(request.Question.Trim(), history),
            cancellationToken);

        Guid.TryParse(response.WorkflowId, out var workflowId);
        return new AskCharakaResponse(
            response.Answer ?? "",
            response.Refused,
            workflowId);
    }

    public async Task<WorkflowExecutionDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var execution = await _executions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowExecution), id);
        return WorkflowExecutionService.ToDto(execution);
    }

    public async Task<WorkflowExecutionDto> ApproveAsync(
        Guid id,
        ApproveAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        await _actors.RequireStaffAsync(cancellationToken);
        var execution = await _executions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowExecution), id);
        if (string.Equals(request.Decision, "RevisionRequested", StringComparison.OrdinalIgnoreCase))
        {
            execution.ApprovalStatus = WorkflowApprovalStatus.RevisionRequested;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return WorkflowExecutionService.ToDto(execution);
        }

        if (execution.RelatedEntityId is null)
        {
            execution.ApprovalStatus = WorkflowApprovalStatus.NotRequired;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new DomainException("This workflow has no related record to approve.");
        }

        if (execution.RelatedEntityType == "AdmissionRequest")
        {
            await _wards.DecideAdmissionAsync(
                execution.RelatedEntityId.Value,
                new AdmissionDecisionRequest(request.Approve, user.Id),
                cancellationToken);
        }
        else if (execution.RelatedEntityType is "FeedbackReply" or "Feedback")
        {
            var replyId = execution.RelatedEntityId.Value;
            if (execution.RelatedEntityType == "Feedback")
            {
                var draft = await _replyRecords.FindLatestAiDraftAsync(replyId, cancellationToken);
                if (draft is null)
                {
                    if (!request.Approve)
                    {
                        execution.ApprovalStatus = WorkflowApprovalStatus.Rejected;
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        return WorkflowExecutionService.ToDto(execution);
                    }

                    throw new DomainException(
                        "No AI reply draft is waiting. Open Feedback and request an AI draft before approving.");
                }

                replyId = draft.Id;
            }
            await _replies.DecideDraftAsync(
                replyId,
                new ReplyDecisionRequest(request.Approve ? ReplyDecision.Approve : ReplyDecision.Reject, request.Reply),
                cancellationToken);
        }
        else
        {
            execution.ApprovalStatus = WorkflowApprovalStatus.NotRequired;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new DomainException(
                $"Approval is not defined for {execution.RelatedEntityType}. Filter AI approvals to Pending bed or feedback plans.");
        }

        execution.ApprovalStatus = request.Approve
            ? WorkflowApprovalStatus.Approved
            : WorkflowApprovalStatus.Rejected;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WorkflowExecutionService.ToDto(execution);
    }
}
