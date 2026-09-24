using FluentValidation;
using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/appointments")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointments;
    private readonly IValidator<CreateAppointmentRequest> _createValidator;
    private readonly IValidator<UpdateAppointmentStatusRequest> _statusValidator;

    public AppointmentsController(
        IAppointmentService appointments,
        IValidator<CreateAppointmentRequest> createValidator,
        IValidator<UpdateAppointmentStatusRequest> statusValidator)
    {
        _appointments = appointments;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> List(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? treatmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _appointments.ListAsync(date, patientId, treatmentId, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _appointments.GetByIdAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _appointments.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(
        Guid id,
        UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _appointments.UpdateStatusAsync(id, request, cancellationToken));
    }
}
