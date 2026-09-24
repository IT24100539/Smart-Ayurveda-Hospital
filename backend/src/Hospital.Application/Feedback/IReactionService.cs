using Hospital.Application.Communication.Dtos;

namespace Hospital.Application.Communication;

public interface IReactionService
{
    /// <summary>
    /// Upsert: a second reaction from the same user replaces the previous one.
    /// </summary>
    Task<ReactionDto> ReactAsync(Guid feedbackId, ReactionRequest request, CancellationToken cancellationToken);

    /// <summary>Removes the signed-in patient's reaction. A missing reaction is a 404.</summary>
    Task RemoveAsync(Guid feedbackId, CancellationToken cancellationToken);
}
