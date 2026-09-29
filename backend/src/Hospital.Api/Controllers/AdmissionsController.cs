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

    public AdmissionsController(IWardService wards, IActorContext actors)
    {
        _wards = wards;
        _actors = actors;
    }

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AdmissionRequestDto>> Create(CreateAdmissionRequestRequest request, CancellationToken cancellationToken)
    {
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
        var ok = await _wards.DecideAdmissionAsync(id, request, cancellationToken);
        if (!ok) return BadRequest(new { message = "Unable to approve admission (no free beds or invalid request)." });
        return NoContent();
    }
}
