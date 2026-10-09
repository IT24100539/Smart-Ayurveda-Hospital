using FluentValidation;
using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/complaints")]
public sealed class ComplaintsController : ControllerBase
{
    private const string StaffRoles = "FrontDeskStaff,Doctor,Admin";

    private readonly IComplaintService _complaints;
    private readonly IValidator<CreateComplaintRequest> _createValidator;
    private readonly IValidator<ComplaintStatusUpdateRequest> _statusValidator;

    public ComplaintsController(
        IComplaintService complaints,
        IValidator<CreateComplaintRequest> createValidator,
        IValidator<ComplaintStatusUpdateRequest> statusValidator)
    {
        _complaints = complaints;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
    }

    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<IReadOnlyList<ComplaintSummaryDto>>> Mine(CancellationToken cancellationToken) =>
        Ok(await _complaints.ListMineAsync(cancellationToken));

    [HttpGet("assignees")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<IReadOnlyList<StaffAssigneeDto>>> Assignees(CancellationToken cancellationToken) =>
        Ok(await _complaints.ListAssigneesAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ComplaintSummaryDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _complaints.GetAsync(id, cancellationToken));

    [HttpGet]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<IReadOnlyList<ComplaintSummaryDto>>> List(
        [FromQuery] bool overdue = false,
        [FromQuery] ComplaintStatus? status = null,
        [FromQuery] ComplaintPriority? priority = null,
        CancellationToken cancellationToken = default)
    {
        if (overdue)
        {
            return Ok(await _complaints.GetOverdueComplaintsAsync(cancellationToken));
        }

        return Ok(await _complaints.ListAsync(status, priority, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ComplaintSummaryDto>> Create(CreateComplaintRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _complaints.CreateAsync(request, cancellationToken);
        return Created($"/api/complaints/{created.Id}", created);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ComplaintSummaryDto>> UpdateStatus(
        Guid id,
        ComplaintStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _complaints.UpdateStatusAsync(id, request, cancellationToken));
    }
}
