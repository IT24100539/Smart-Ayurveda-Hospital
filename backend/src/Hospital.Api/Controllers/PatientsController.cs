using FluentValidation;
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
    private readonly IValidator<CreatePatientRequest> _createValidator;
    private readonly IValidator<UpdatePatientRequest> _updateValidator;

    public PatientsController(
        IPatientService patients,
        IValidator<CreatePatientRequest> createValidator,
        IValidator<UpdatePatientRequest> updateValidator)
    {
        _patients = patients;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientDto>>> Search(
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _patients.SearchAsync(q, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _patients.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<PatientDto>> Create(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _patients.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PatientDto>> Update(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _patients.UpdateAsync(id, request, cancellationToken));
    }
}
