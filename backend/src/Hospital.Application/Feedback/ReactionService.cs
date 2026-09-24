using Hospital.Application.Abstractions;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Communication;

public sealed class ReactionService : IReactionService
{
    private readonly IFeedbackRepository _feedback;
    private readonly IFeedbackReactionRepository _reactions;
    private readonly IActorContext _actors;
    private readonly IUnitOfWork _unitOfWork;

    public ReactionService(
        IFeedbackRepository feedback,
        IFeedbackReactionRepository reactions,
        IActorContext actors,
        IUnitOfWork unitOfWork)
    {
        _feedback = feedback;
        _reactions = reactions;
        _actors = actors;
        _unitOfWork = unitOfWork;
    }

    public async Task<ReactionDto> ReactAsync(Guid feedbackId, ReactionRequest request, CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        await _actors.RequirePatientAsync(cancellationToken);

        var feedback = await _feedback.GetByIdAsync(feedbackId, cancellationToken)
            ?? throw new NotFoundException(nameof(Feedback), feedbackId);
        if (feedback.Status != FeedbackStatus.Visible)
        {
            throw new DomainException("Reactions are only allowed on visible feedback.");
        }

        var existing = await _reactions.GetByUserAsync(feedbackId, user.Id, cancellationToken);
        if (existing is null)
        {
            existing = new FeedbackReaction
            {
                FeedbackId = feedbackId,
                UserId = user.Id,
                ReactionType = request.ReactionType
            };
            await _reactions.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.ReactionType = request.ReactionType;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReactionDto(existing.Id, existing.FeedbackId, existing.ReactionType);
    }

    public async Task RemoveAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        await _actors.RequirePatientAsync(cancellationToken);
        var existing = await _reactions.GetByUserAsync(feedbackId, user.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(FeedbackReaction), feedbackId);
        _reactions.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
