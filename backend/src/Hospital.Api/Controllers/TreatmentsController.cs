using FluentValidation;
using Hospital.Application.Common;
using Hospital.Application.Treatments;
using Hospital.Application.Treatments.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/treatments")]
public sealed class TreatmentsController : ControllerBase
{
    private const string StaffRoles = "Admin,FrontDeskStaff";

    private readonly ITreatmentService _treatments;
    private readonly IValidator<TreatmentSearchQuery> _searchValidator;
    private readonly IValidator<CreateTreatmentRequest> _createValidator;
    private readonly IValidator<UpdateTreatmentRequest> _updateValidator;
    private readonly IValidator<CreateScheduleEntryRequest> _createScheduleValidator;
    private readonly IValidator<UpdateScheduleEntryRequest> _updateScheduleValidator;

    public TreatmentsController(
        ITreatmentService treatments,
        IValidator<TreatmentSearchQuery> searchValidator,
        IValidator<CreateTreatmentRequest> createValidator,
        IValidator<UpdateTreatmentRequest> updateValidator,
        IValidator<CreateScheduleEntryRequest> createScheduleValidator,
        IValidator<UpdateScheduleEntryRequest> updateScheduleValidator)
    {
        _treatments = treatments;
        _searchValidator = searchValidator;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _createScheduleValidator = createScheduleValidator;
        _updateScheduleValidator = updateScheduleValidator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<TreatmentSummaryDto>>> Search(
        [FromQuery] TreatmentSearchQuery query,
        CancellationToken cancellationToken)
    {
        await _searchValidator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await _treatments.SearchAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TreatmentDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _treatments.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<TreatmentDetailDto>> Create(
        CreateTreatmentRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _treatments.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<TreatmentDetailDto>> Update(
        Guid id,
        UpdateTreatmentRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _treatments.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TreatmentDetailDto>> Deactivate(Guid id, CancellationToken cancellationToken) =>
        Ok(await _treatments.DeactivateAsync(id, cancellationToken));

    [HttpPost("{id:guid}/schedule")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ScheduleEntryDto>> AddSchedule(
        Guid id,
        CreateScheduleEntryRequest request,
        CancellationToken cancellationToken)
    {
        await _createScheduleValidator.ValidateAndThrowAsync(request, cancellationToken);
        var entry = await _treatments.AddScheduleEntryAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id }, entry);
    }

    [HttpPut("{id:guid}/schedule/{entryId:guid}")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ScheduleEntryDto>> UpdateSchedule(
        Guid id,
        Guid entryId,
        UpdateScheduleEntryRequest request,
        CancellationToken cancellationToken)
    {
        await _updateScheduleValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _treatments.UpdateScheduleEntryAsync(id, entryId, request, cancellationToken));
    }

    [HttpDelete("{id:guid}/schedule/{entryId:guid}")]
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> RemoveSchedule(Guid id, Guid entryId, CancellationToken cancellationToken)
    {
        await _treatments.RemoveScheduleEntryAsync(id, entryId, cancellationToken);
        return NoContent();
    }
}
