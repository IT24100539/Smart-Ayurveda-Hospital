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
    private readonly IValidator<AskTreatmentInfoRequest> _askValidator;

    public AgentWorkflowsController(
        IAgentWorkflowService workflows,
        IValidator<StartAgentWorkflowRequest> startValidator,
        IValidator<ApproveAgentWorkflowRequest> approveValidator,
        IValidator<AskTreatmentInfoRequest> askValidator)
    {
        _workflows = workflows;
        _startValidator = startValidator;
        _approveValidator = approveValidator;
        _askValidator = askValidator;
    }

    [HttpPost("start")]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<CoordinatorAgentResponse>> Start(
        StartAgentWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        await _startValidator.ValidateAndThrowAsync(request, cancellationToken);
        var started = await _workflows.StartAsync(request, cancellationToken);
        return Ok(started);
    }

    /// <summary>
    /// Patient ask for the treatment-info agent (listed therapies, days, fees).
    /// Medical-advice questions come back refused. The agent does not invent catalogue rows.
    /// </summary>
    [HttpPost("ask-treatment")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AskTreatmentInfoResponse>> AskTreatment(
        AskTreatmentInfoRequest request,
        CancellationToken cancellationToken)
    {
        await _askValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _workflows.AskTreatmentAsync(request, cancellationToken));
    }

    /// <summary>
    /// Patient ask for the patient-info agent (administrative details such as UHID, district, address).
    /// Medical-advice questions come back refused.
    /// </summary>
    [HttpPost("ask-patient")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AskPatientInfoResponse>> AskPatient(
        AskPatientInfoRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required.");
        }
        return Ok(await _workflows.AskPatientInfoAsync(request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
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
