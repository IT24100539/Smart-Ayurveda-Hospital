using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/appointments")]
public sealed class AppointmentActionsController : ControllerBase
{
    private readonly IAppointmentService _appointments;

    public AppointmentActionsController(IAppointmentService appointments) => _appointments = appointments;

    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> MyAppointments([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // Try to read patient id from claims (sub or custom). Fallback: return bad request.
        var sub = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var patientId)) return BadRequest(new { message = "Unable to determine patient id from token." });
        var result = await _appointments.ListAsync(null, patientId, null, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/decision")]
    [Authorize(Roles = "FrontDeskStaff,Admin,Doctor")]
    public async Task<ActionResult<AppointmentDto>> Decide(Guid id, AppointmentDecisionRequest request, CancellationToken cancellationToken)
    {
        // Map to existing UpdateAppointmentStatusRequest
        var update = new UpdateAppointmentStatusRequest(request.Status, request.DecidedBy);
        var res = await _appointments.UpdateStatusAsync(id, update, cancellationToken);
        return Ok(res);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var sub = User.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var patientId)) return BadRequest(new { message = "Unable to determine patient id from token." });
        await _appointments.CancelAsync(id, patientId, cancellationToken);
        return NoContent();
    }
}

public sealed record AppointmentDecisionRequest(AppointmentStatus Status, Guid? DecidedBy);
