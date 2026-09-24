using FluentValidation;
using Hospital.Application.Common;
using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/feedback")]
public sealed class FeedbackController : ControllerBase
{
    private const string StaffRoles = "FrontDeskStaff,Doctor,Admin";

    private readonly IFeedbackService _feedback;
    private readonly IReactionService _reactions;
    private readonly IReplyService _replies;
    private readonly IValidator<CreateFeedbackRequest> _createValidator;
    private readonly IValidator<UpdateFeedbackRequest> _updateValidator;
    private readonly IValidator<ModerateFeedbackRequest> _moderateValidator;
    private readonly IValidator<FeedbackSearchQuery> _searchValidator;
    private readonly IValidator<ReactionRequest> _reactionValidator;
    private readonly IValidator<CreateReplyRequest> _replyValidator;

    public FeedbackController(
        IFeedbackService feedback,
        IReactionService reactions,
        IReplyService replies,
        IValidator<CreateFeedbackRequest> createValidator,
        IValidator<UpdateFeedbackRequest> updateValidator,
        IValidator<ModerateFeedbackRequest> moderateValidator,
        IValidator<FeedbackSearchQuery> searchValidator,
        IValidator<ReactionRequest> reactionValidator,
        IValidator<CreateReplyRequest> replyValidator)
    {
        _feedback = feedback;
        _reactions = reactions;
        _replies = replies;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _moderateValidator = moderateValidator;
        _searchValidator = searchValidator;
        _reactionValidator = reactionValidator;
        _replyValidator = replyValidator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PublicFeedbackDto>>> PublicFeed(CancellationToken cancellationToken) =>
        Ok(await _feedback.GetPublicFeedAsync(cancellationToken));

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<IReadOnlyList<PatientFeedbackDto>>> Mine(CancellationToken cancellationToken) =>
        Ok(await _feedback.ListMineAsync(cancellationToken));

    [HttpGet("summary")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<FeedbackStatsDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await _feedback.GetStatsAsync(cancellationToken));

    [HttpGet("staff")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<PagedResult<FeedbackSummaryDto>>> Search(
        [FromQuery] StaffFeedbackQuery query,
        CancellationToken cancellationToken)
    {
        var search = query.ToSearch();
        await _searchValidator.ValidateAndThrowAsync(search, cancellationToken);
        return Ok(await _feedback.SearchAsync(search, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<FeedbackDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await _feedback.GetForStaffAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<FeedbackDetailDto>> Create(CreateFeedbackRequest request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _feedback.CreateAsync(request, cancellationToken);
        return Created($"/api/feedback/{created.Id}", created);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<FeedbackDetailDto>> Edit(Guid id, UpdateFeedbackRequest request, CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _feedback.EditAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:guid}/moderate")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<FeedbackDetailDto>> Moderate(
        Guid id,
        ModerateFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        await _moderateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _feedback.ModerateAsync(id, request.Action, cancellationToken));
    }

    [HttpPost("{id:guid}/reactions")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ReactionDto>> React(Guid id, ReactionRequest request, CancellationToken cancellationToken)
    {
        await _reactionValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _reactions.ReactAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}/reactions")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> RemoveReaction(Guid id, CancellationToken cancellationToken)
    {
        await _reactions.RemoveAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/replies/patient")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<ReplyDto>> PatientReply(Guid id, CreateReplyRequest request, CancellationToken cancellationToken)
    {
        await _replyValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _replies.CreatePatientReplyAsync(id, request, cancellationToken);
        return Created($"/api/replies/{created.Id}", created);
    }

    [HttpPost("{id:guid}/replies")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ReplyDto>> Reply(Guid id, CreateReplyRequest request, CancellationToken cancellationToken)
    {
        await _replyValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _replies.CreateManualReplyAsync(id, request, cancellationToken);
        return Created($"/api/replies/{created.Id}", created);
    }

    [HttpPost("{id:guid}/replies/ai-draft")]
    [Authorize(Roles = StaffRoles)]
    public async Task<ActionResult<ReplyDto>> RequestAiDraft(Guid id, CancellationToken cancellationToken)
    {
        var draft = await _replies.RequestAiDraftAsync(id, cancellationToken);
        return Created($"/api/replies/{draft.Id}", draft);
    }
}

/// <summary>
/// Query-string shape for the staff search. Bound separately so page defaults apply when omitted.
/// </summary>
public sealed class StaffFeedbackQuery
{
    public Hospital.Domain.Enums.FeedbackStatus? Status { get; set; }
    public int? Rating { get; set; }
    public Hospital.Domain.Enums.FeedbackCategory? Category { get; set; }
    public Hospital.Domain.Enums.FeedbackSentiment? Sentiment { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
    public string? Search { get; set; }

    public FeedbackSearchQuery ToSearch() => new(Status, Rating, Category, Sentiment, Page, PageSize, Sort, Search);
}
