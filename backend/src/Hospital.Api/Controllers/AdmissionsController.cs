using Hospital.Application.Wards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/admissions")]
public sealed class AdmissionsController : ControllerBase
{
    private readonly IWardService _wards;

    public AdmissionsController(IWardService wards) => _wards = wards;

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AdmissionRequestDto>> Create(CreateAdmissionRequestRequest request, CancellationToken cancellationToken)
    {
        var created = await _wards.RequestAdmissionAsync(request, cancellationToken);
        return CreatedAtAction(null, created);
    }

    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<IReadOnlyList<AdmissionRequestDto>>> Pending(CancellationToken cancellationToken)
    {
        var list = await _wards.ListPendingAdmissionsAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPatch("{id:guid}/decision")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult> Decide(Guid id, AdmissionDecisionRequest request, CancellationToken cancellationToken)
    {
        var ok = await _wards.DecideAdmissionAsync(id, request, cancellationToken);
        if (!ok) return BadRequest(new { message = "Unable to approve admission (no free beds or invalid request)." });
        return NoContent();
    }
}
