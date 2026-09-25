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
                var draft = await _replyRecords.FindLatestAiDraftAsync(replyId, cancellationToken)
                    ?? throw new NotFoundException(nameof(FeedbackReply), replyId);
                replyId = draft.Id;
            }
            await _replies.DecideDraftAsync(
                replyId,
                new ReplyDecisionRequest(request.Approve ? ReplyDecision.Approve : ReplyDecision.Reject, request.Reply),
                cancellationToken);
        }
        else
        {
            throw new DomainException($"Approval is not defined for {execution.RelatedEntityType}.");
        }

        execution.ApprovalStatus = request.Approve
            ? WorkflowApprovalStatus.Approved
            : WorkflowApprovalStatus.Rejected;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return WorkflowExecutionService.ToDto(execution);
    }
}
