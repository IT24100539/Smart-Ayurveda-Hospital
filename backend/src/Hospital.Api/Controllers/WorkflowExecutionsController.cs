using FluentValidation;
using Hospital.Application.Common;
using Hospital.Application.Workflows;
using Hospital.Application.Workflows.Dtos;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = "FrontDeskStaff,Doctor,Admin")]
[Route("api/workflow-executions")]
public sealed class WorkflowExecutionsController : ControllerBase
{
    private readonly IWorkflowExecutionService _workflows;
    private readonly IValidator<WorkflowExecutionSearchQuery> _searchValidator;

    public WorkflowExecutionsController(
        IWorkflowExecutionService workflows,
        IValidator<WorkflowExecutionSearchQuery> searchValidator)
    {
        _workflows = workflows;
        _searchValidator = searchValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkflowExecutionDto>>> Search(
        [FromQuery] StaffWorkflowExecutionQuery query,
        CancellationToken cancellationToken)
    {
        var search = query.ToSearch();
        await _searchValidator.ValidateAndThrowAsync(search, cancellationToken);
        return Ok(await _workflows.SearchAsync(search, cancellationToken));
    }
}

public sealed class StaffWorkflowExecutionQuery
{
    public string? AgentName { get; set; }
    public WorkflowApprovalStatus? ApprovalStatus { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public WorkflowExecutionSearchQuery ToSearch() =>
        new(AgentName, ApprovalStatus, From, To, Search, Sort, Page, PageSize);
}
