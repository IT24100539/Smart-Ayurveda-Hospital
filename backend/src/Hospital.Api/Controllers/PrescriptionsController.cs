using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Application.Common;
using Hospital.Application.Prescriptions;
using Hospital.Application.Prescriptions.Dtos;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

/// <summary>
/// Herbal prescriptions written by a doctor for one patient visit.
/// Patients read only their own issued chart. Front desk cannot open it.
/// Doctors, therapists, and admins can read. Only a doctor can write.
/// </summary>
[ApiController]
[Authorize]
[Route("api/prescriptions")]
public sealed class PrescriptionsController : ControllerBase
{
    private const string ClinicalReaders = "Admin,Doctor,Therapist";
    private const string Authors = nameof(UserRole.Doctor);

    private readonly IPrescriptionService _prescriptions;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreatePrescriptionRequest> _createValidator;
    private readonly IValidator<UpdatePrescriptionRequest> _updateValidator;
    private readonly IValidator<RevisePrescriptionRequest> _reviseValidator;

    public PrescriptionsController(
        IPrescriptionService prescriptions,
        IAuditLogService audit,
        IValidator<CreatePrescriptionRequest> createValidator,
        IValidator<UpdatePrescriptionRequest> updateValidator,
        IValidator<RevisePrescriptionRequest> reviseValidator)
    {
        _prescriptions = prescriptions;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _reviseValidator = reviseValidator;
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(PagedResult<PrescriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PrescriptionDto>>> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _prescriptions.ListMineAsync(page, pageSize, cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = ClinicalReaders)]
    [ProducesResponseType(typeof(PagedResult<PrescriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PrescriptionDto>>> List(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? appointmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _prescriptions.ListAsync(patientId, appointmentId, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrescriptionDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetAsync(id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.ClinicalRecord, id.ToString(), cancellationToken);
        return Ok(prescription);
    }

    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(PrescriptionHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrescriptionHistoryDto>> History(Guid id, CancellationToken cancellationToken)
    {
        var history = await _prescriptions.GetHistoryAsync(id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.ClinicalRecord, id.ToString(), cancellationToken);
        return Ok(history);
    }

    [HttpPost]
    [Authorize(Roles = Authors)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrescriptionDto>> Create(
        [FromBody] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _prescriptions.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Authors)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrescriptionDto>> Update(
        Guid id,
        [FromBody] UpdatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _prescriptions.UpdateDraftAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/issue")]
    [Authorize(Roles = Authors)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrescriptionDto>> Issue(Guid id, CancellationToken cancellationToken) =>
        Ok(await _prescriptions.IssueAsync(id, cancellationToken));

    [HttpPost("{id:guid}/revise")]
    [Authorize(Roles = Authors)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrescriptionDto>> Revise(
        Guid id,
        [FromBody] RevisePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        await _reviseValidator.ValidateAndThrowAsync(request, cancellationToken);
        var revised = await _prescriptions.ReviseAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = revised.Id }, revised);
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = Authors)]
    [ProducesResponseType(typeof(PrescriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrescriptionDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await _prescriptions.CancelAsync(id, cancellationToken));
}
