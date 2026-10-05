using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Appointments.Dtos;
using Hospital.Application.Audit;
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
    private readonly IActorContext _actors;
    private readonly IAuditLogService _audit;
    private readonly IValidator<CreateAppointmentRequest> _createValidator;
    private readonly IValidator<UpdateAppointmentStatusRequest> _statusValidator;
    private readonly IValidator<RescheduleAppointmentRequest> _rescheduleValidator;

    public AppointmentsController(
        IAppointmentService appointments,
        IActorContext actors,
        IAuditLogService audit,
        IValidator<CreateAppointmentRequest> createValidator,
        IValidator<UpdateAppointmentStatusRequest> statusValidator,
        IValidator<RescheduleAppointmentRequest> rescheduleValidator)
    {
        _appointments = appointments;
        _actors = actors;
        _audit = audit;
        _createValidator = createValidator;
        _statusValidator = statusValidator;
        _rescheduleValidator = rescheduleValidator;
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> Mine(CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        return Ok(await _appointments.ListAsync(null, patient.Id, null, 1, 50, cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<PagedResult<AppointmentDto>>> List(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? treatmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        return Ok(await _appointments.ListAsync(date, patientId, treatmentId, page, pageSize, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppointmentDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var appt = await _appointments.GetByIdAsync(id, cancellationToken);
        if (User.IsInRole("Patient"))
        {
            var patient = await _actors.RequirePatientAsync(cancellationToken);
            if (appt.PatientId != patient.Id)
            {
                return Forbid();
            }
        }
        else if (!User.IsInRole("Admin") && !User.IsInRole("Doctor") && !User.IsInRole("FrontDeskStaff"))
        {
            return Forbid();
        }

        await _audit.RecordAsync(AuditActions.View, AuditEntities.Appointment, id.ToString(), cancellationToken);
        return Ok(appt);
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        if (User.IsInRole("Patient"))
        {
            var patient = await _actors.RequirePatientAsync(cancellationToken);
            request = request with { PatientId = patient.Id };
        }

        var created = await _appointments.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Doctor,FrontDeskStaff")]
    public async Task<ActionResult<AppointmentDto>> UpdateStatus(
        Guid id,
        UpdateAppointmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _appointments.UpdateStatusAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:guid}/reschedule")]
    public async Task<ActionResult<AppointmentDto>> Reschedule(
        Guid id,
        RescheduleAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        await _rescheduleValidator.ValidateAndThrowAsync(request, cancellationToken);
        Guid? requestingPatientId = User.IsInRole("Patient")
            ? (await _actors.RequirePatientAsync(cancellationToken)).Id
            : null;
        return Ok(await _appointments.RescheduleAsync(id, request, requestingPatientId, cancellationToken));
    }
}
