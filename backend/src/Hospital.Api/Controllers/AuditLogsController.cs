using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogs;

    public AuditLogsController(IAuditLogService auditLogs)
    {
        _auditLogs = auditLogs;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> List(
        [FromQuery] string? query,
        [FromQuery] string? action,
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        QueryLimits.EnsurePage(page, pageSize);
        QueryLimits.EnsureLength("query", query);
        QueryLimits.EnsureLength("action", action, 64);
        QueryLimits.EnsureLength("entityName", entityName, 64);
        QueryLimits.EnsureLength("entityId", entityId, 64);
        QueryLimits.EnsureDateRange(fromDate, toDate);
        return Ok(await _auditLogs.SearchAsync(
            query, action, entityName, entityId, fromDate, toDate, page, pageSize, cancellationToken));
    }
}
