using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Microsoft.AspNetCore.Authorization.Authorize(Policy = Hospital.Api.Authentication.InternalServiceAuthenticationHandler.PolicyName)]
[Route("api/internal")]
public sealed class InternalController : ControllerBase
{
    private readonly IValidator<CheckSlotRequest> _checkSlotValidator;

    public InternalController(IValidator<CheckSlotRequest> checkSlotValidator) =>
        _checkSlotValidator = checkSlotValidator;

    // Called by the internal scheduling agent with the X-Internal-Service-Key header.
    [HttpPost("appointments/check-slot")]
    public async Task<ActionResult> CheckSlot([FromBody] CheckSlotRequest request, CancellationToken cancellationToken)
    {
        await _checkSlotValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok();
    }
}

public sealed class CheckSlotRequest
{
    public Guid? PatientId { get; init; }
    public Guid? TreatmentId { get; init; }
    public DateOnly? RequestedDate { get; init; }
    public string? RequestedTimeSlot { get; init; }
}
