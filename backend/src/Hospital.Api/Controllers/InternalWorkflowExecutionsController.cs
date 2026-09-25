using Hospital.Api.Security;
using Hospital.Application.Workflows;
using Hospital.Application.Workflows.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

/// <summary>
/// Agent workflow persistence. Called by agent-service with X-Internal-Service-Key.
/// </summary>
[ApiController]
[AllowAnonymous]
[ServiceFilter(typeof(InternalServiceKeyFilter))]
[Route("api/internal/workflow-executions")]
public sealed class InternalWorkflowExecutionsController : ControllerBase
{
    private readonly IWorkflowExecutionService _workflows;
    private readonly IValidator<UpsertWorkflowExecutionRequest> _upsertValidator;
    private readonly IValidator<UpdateWorkflowExecutionRequest> _updateValidator;

    public InternalWorkflowExecutionsController(
        IWorkflowExecutionService workflows,
        IValidator<UpsertWorkflowExecutionRequest> upsertValidator,
        IValidator<UpdateWorkflowExecutionRequest> updateValidator)
    {
        _workflows = workflows;
        _upsertValidator = upsertValidator;
        _updateValidator = updateValidator;
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowExecutionDto>> Upsert(
        UpsertWorkflowExecutionRequest request,
        CancellationToken cancellationToken)
    {
        await _upsertValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _workflows.UpsertAsync(request, cancellationToken));
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<WorkflowExecutionDto>> Update(
        Guid id,
        UpdateWorkflowExecutionRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _workflows.UpdateAsync(id, request, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowExecutionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _workflows.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}
