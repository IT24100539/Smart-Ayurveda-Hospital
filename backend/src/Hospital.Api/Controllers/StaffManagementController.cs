using System.Security.Claims;
using Hospital.Application.StaffManagement;
using Hospital.Application.StaffManagement.Dtos;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/admin/staff")]
[Authorize(Roles = "Admin")]
public sealed class StaffManagementController : ControllerBase
{
    private readonly IStaffManagementService _staffService;

    public StaffManagementController(IStaffManagementService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StaffListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<StaffListResponse>> ListStaff(
        [FromQuery] string? query,
        [FromQuery] UserRole? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _staffService.ListStaffAsync(query, role, isActive, page, pageSize, cancellationToken);
        return Ok(new StaffListResponse(items, total, page, pageSize));
    }

    [HttpPost]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffUserDto>> CreateStaff(
        [FromBody] CreateStaffUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var actorEmail = GetActorEmail();

        var created = await _staffService.CreateStaffAsync(request, actorUserId, actorEmail, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffUserDto>> UpdateRole(
        [FromRoute] Guid id,
        [FromBody] UpdateStaffRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var actorEmail = GetActorEmail();

        var updated = await _staffService.UpdateRoleAsync(id, request, actorUserId, actorEmail, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(StaffUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffUserDto>> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateStaffStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var actorEmail = GetActorEmail();

        var updated = await _staffService.UpdateStatusAsync(id, request, actorUserId, actorEmail, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/force-password-reset")]
    [ProducesResponseType(typeof(ForcePasswordResetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForcePasswordResetResponse>> ForcePasswordReset(
        [FromRoute] Guid id,
        [FromBody] ForcePasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = GetActorUserId();
        var actorEmail = GetActorEmail();

        var response = await _staffService.ForcePasswordResetAsync(id, request, actorUserId, actorEmail, cancellationToken);
        return Ok(response);
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(AuditLogListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AuditLogListResponse>> ListAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _staffService.ListAuditLogsAsync(page, pageSize, cancellationToken);
        return Ok(new AuditLogListResponse(items, total, page, pageSize));
    }

    private Guid GetActorUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private string GetActorEmail() =>
        User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name) ?? "admin";
}

public sealed record StaffListResponse(
    IReadOnlyList<StaffUserDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AuditLogListResponse(
    IReadOnlyList<AuditLogDto> Items,
    int Total,
    int Page,
    int PageSize);
