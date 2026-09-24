using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Hospital.Api.Security;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

/// <summary>
/// Read-only feedback lookups for the internal agent service. Not a staff route.
/// </summary>
[ApiController]
[AllowAnonymous]
[ServiceFilter(typeof(InternalServiceKeyFilter))]
[Route("api/internal/feedback")]
public sealed class InternalFeedbackController : ControllerBase
{
    private readonly IInternalFeedbackService _feedback;

    public InternalFeedbackController(IInternalFeedbackService feedback) => _feedback = feedback;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InternalFeedbackContextDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _feedback.GetAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet("similar")]
    public async Task<ActionResult<SimilarFeedbackCountDto>> Similar(
        [FromQuery] FeedbackCategory category,
        [FromQuery] Guid excludePatientId,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(category) || excludePatientId == Guid.Empty)
        {
            return BadRequest(new { detail = "category and excludePatientId are required." });
        }

        var count = await _feedback.CountSimilarAsync(category, excludePatientId, cancellationToken);
        return Ok(new SimilarFeedbackCountDto(count, category, excludePatientId));
    }
}
