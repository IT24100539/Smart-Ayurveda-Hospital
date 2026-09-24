using Hospital.Application.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = Hospital.Api.Authentication.InternalServiceAuthenticationHandler.PolicyName)]
[Route("api/internal")]
public sealed class InternalController : ControllerBase
{
    private readonly IAppointmentService _appointments;

    public InternalController(IAppointmentService appointments) => _appointments = appointments;

    // This endpoint is intended to be called by the internal Scheduling & Bed Agent using the X-Internal-Service-Key header.
    [HttpPost("appointments/check-slot")]
    public Task<ActionResult> CheckSlot([FromBody] object payload)
    {
        // For now this is a thin passthrough placeholder. The agent should call a dedicated endpoint later.
        return Task.FromResult<ActionResult>(Ok());
    }
}
