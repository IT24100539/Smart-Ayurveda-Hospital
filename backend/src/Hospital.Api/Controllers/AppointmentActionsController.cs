using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Common;
using Hospital.Application.Appointments.Dtos;
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
    private readonly IActorContext _actors;
    private readonly IValidator<AppointmentDecisionRequest> _decisionValidator;

    public AppointmentActionsController(
        IAppointmentService appointments,
        IActorContext actors,
        IValidator<AppointmentDecisionRequest> decisionValidator)
    {
        _appointments = appointments;
        _actors = actors;
        _decisionValidator = decisionValidator;
    }

    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> MyAppointments([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var result = await _appointments.ListAsync(null, patient.Id, null, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/decision")]
    [Authorize(Roles = "FrontDeskStaff,Admin,Doctor")]
    public async Task<ActionResult<AppointmentDto>> Decide(Guid id, AppointmentDecisionRequest request, CancellationToken cancellationToken)
    {
        await _decisionValidator.ValidateAndThrowAsync(request, cancellationToken);
        var update = new UpdateAppointmentStatusRequest(request.Status, request.DecidedBy);
        var res = await _appointments.UpdateStatusAsync(id, update, cancellationToken);
        return Ok(res);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        await _appointments.CancelAsync(id, patient.Id, cancellationToken);
        return NoContent();
    }
}

public sealed record AppointmentDecisionRequest(AppointmentStatus Status, Guid? DecidedBy);
