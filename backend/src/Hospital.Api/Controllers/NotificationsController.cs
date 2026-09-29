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
    private readonly IActorContext _actors;

    public NotificationsController(INotificationService notifications, IActorContext actors)
    {
        _notifications = notifications;
        _actors = actors;
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
