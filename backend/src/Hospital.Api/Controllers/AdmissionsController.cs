using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Wards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/admissions")]
public sealed class AdmissionsController : ControllerBase
{
    private readonly IWardService _wards;
    private readonly IActorContext _actors;
    private readonly IValidator<CreateAdmissionRequestRequest> _createValidator;
    private readonly IValidator<AdmissionDecisionRequest> _decisionValidator;

    public AdmissionsController(
        IWardService wards,
        IActorContext actors,
        IValidator<CreateAdmissionRequestRequest> createValidator,
        IValidator<AdmissionDecisionRequest> decisionValidator)
    {
        _wards = wards;
        _actors = actors;
        _createValidator = createValidator;
        _decisionValidator = decisionValidator;
    }

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AdmissionRequestDto>> Create(CreateAdmissionRequestRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var created = await _wards.RequestAdmissionAsync(request with { PatientId = patient.Id }, cancellationToken);
        return CreatedAtAction(null, created);
    }

    [HttpGet]
    [Authorize(Roles = "FrontDeskStaff,Doctor,Admin")]
    public async Task<ActionResult<IReadOnlyList<AdmissionRequestDto>>> Pending(CancellationToken cancellationToken)
    {
        var list = await _wards.ListPendingAdmissionsAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPatch("{id:guid}/decision")]
    [Authorize(Roles = "FrontDeskStaff,Doctor,Admin")]
    public async Task<ActionResult> Decide(Guid id, AdmissionDecisionRequest request, CancellationToken cancellationToken)
    {
        await _decisionValidator.ValidateAndThrowAsync(request, cancellationToken);
        var ok = await _wards.DecideAdmissionAsync(id, request, cancellationToken);
        if (!ok)
        {
            throw new Hospital.Domain.Exceptions.DomainException("Unable to approve admission (no free beds or invalid request).");
        }
        return NoContent();
    }
}
