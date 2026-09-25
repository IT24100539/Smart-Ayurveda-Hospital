using FluentValidation;
using Hospital.Application.Agents;
using Hospital.Application.Agents.Dtos;
using Hospital.Application.Workflows.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/agent-workflows")]
public sealed class AgentWorkflowsController : ControllerBase
{
    private readonly IAgentWorkflowService _workflows;
    private readonly IValidator<StartAgentWorkflowRequest> _startValidator;
    private readonly IValidator<ApproveAgentWorkflowRequest> _approveValidator;

    public AgentWorkflowsController(
        IAgentWorkflowService workflows,
        IValidator<StartAgentWorkflowRequest> startValidator,
        IValidator<ApproveAgentWorkflowRequest> approveValidator)
    {
        _workflows = workflows;
        _startValidator = startValidator;
        _approveValidator = approveValidator;
    }

    [HttpPost("start")]
    public async Task<ActionResult<CoordinatorAgentResponse>> Start(
        StartAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        await _startValidator.ValidateAndThrowAsync(request, cancellationToken);
        var started = await _workflows.StartAsync(request, cancellationToken);
        return Ok(started);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowExecutionDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _workflows.GetAsync(id, cancellationToken));

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Admin,FrontDeskStaff,Doctor")]
    public async Task<ActionResult<WorkflowExecutionDto>> Approve(
        Guid id,
        ApproveAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        await _approveValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _workflows.ApproveAsync(id, request, cancellationToken));
    }
}
