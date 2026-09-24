using FluentValidation;
using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize(Roles = "FrontDeskStaff,Doctor,Admin")]
[Route("api/replies")]
public sealed class RepliesController : ControllerBase
{
    private readonly IReplyService _replies;
    private readonly IValidator<ReplyDecisionRequest> _decisionValidator;

    public RepliesController(IReplyService replies, IValidator<ReplyDecisionRequest> decisionValidator)
    {
        _replies = replies;
        _decisionValidator = decisionValidator;
    }

    [HttpPatch("{id:guid}/decision")]
    public async Task<ActionResult<ReplyDto>> Decide(Guid id, ReplyDecisionRequest request, CancellationToken cancellationToken)
    {
        await _decisionValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _replies.DecideDraftAsync(id, request, cancellationToken));
    }
}
