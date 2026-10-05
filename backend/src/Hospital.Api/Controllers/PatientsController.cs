using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Audit;
using Hospital.Application.Common;
using Hospital.Application.Patients;
using Hospital.Application.Patients.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/patients")]
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientService _patients;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreatePatientRequest> _createValidator;
    private readonly IValidator<UpdatePatientRequest> _updateValidator;

    public PatientsController(
        IPatientService patients,
        IAuditLogService audit,
        IValidator<CreatePatientRequest> createValidator,
        IValidator<UpdatePatientRequest> updateValidator)
    {
        _patients = patients;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<PagedResult<PatientDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        QueryLimits.EnsureLength("q", q);
        return Ok(await _patients.SearchAsync(q, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<PatientDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(id, cancellationToken);
        await _audit.RecordAsync(AuditActions.View, AuditEntities.Patient, id.ToString(), cancellationToken);
        return Ok(patient);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _patients.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _patients.UpdateAsync(id, request, cancellationToken));
    }
}
