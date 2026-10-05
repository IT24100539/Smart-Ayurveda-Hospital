using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly IDeviceTokenService _devices;
    private readonly IActorContext _actors;
    private readonly IValidator<RegisterDeviceTokenRequest> _registerDeviceValidator;

    public NotificationsController(
        INotificationService notifications,
        IDeviceTokenService devices,
        IActorContext actors,
        IValidator<RegisterDeviceTokenRequest> registerDeviceValidator)
    {
        _notifications = notifications;
        _devices = devices;
        _actors = actors;
        _registerDeviceValidator = registerDeviceValidator;
    }

    [HttpPost("device-tokens")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<DeviceTokenDto>> RegisterDevice(
        RegisterDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _registerDeviceValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _devices.RegisterAsync(request, cancellationToken));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Mine(CancellationToken cancellationToken)
    {
        var chartId = await _actors.RequirePatientIdAsync(cancellationToken);
        return Ok(await _notifications.GetForPatient(chartId, cancellationToken));
    }

    [HttpPatch("{id:guid}/read")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<NotificationDto>> MarkRead(Guid id, CancellationToken cancellationToken) =>
        Ok(await _notifications.MarkReadAsync(id, cancellationToken));

    [HttpPatch("read-all")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<MarkAllReadResult>> MarkAllRead(CancellationToken cancellationToken) =>
        Ok(await _notifications.MarkAllReadAsync(cancellationToken));

    [HttpGet("staff")]
    [Authorize(Roles = "FrontDeskStaff,Doctor,Admin")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Staff(CancellationToken cancellationToken) =>
        Ok(await _notifications.GetForStaffAsync(cancellationToken));
}
