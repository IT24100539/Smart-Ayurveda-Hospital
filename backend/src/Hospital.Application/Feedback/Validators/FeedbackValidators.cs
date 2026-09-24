using FluentValidation;
using Hospital.Application.Communication.Dtos;

namespace Hospital.Application.Communication.Validators;

public sealed class CreateFeedbackRequestValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AppointmentId!.Value).NotEmpty().When(x => x.AppointmentId is not null);
        RuleFor(x => x.TreatmentId!.Value).NotEmpty().When(x => x.TreatmentId is not null);
        RuleFor(x => x)
            .Must(x => x.AppointmentId is not null || x.TreatmentId is not null)
            .WithMessage("Link the comment to a completed appointment or to a treatment.");
    }
}

public sealed class UpdateFeedbackRequestValidator : AbstractValidator<UpdateFeedbackRequest>
{
    public UpdateFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating is not null);
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000).When(x => x.Comment is not null);
        RuleFor(x => x)
            .Must(x => x.Withdraw || x.Rating is not null || x.Comment is not null || x.IsAnonymous is not null)
            .WithMessage("Provide a change or withdraw the feedback.");
    }
}

public sealed class ModerateFeedbackRequestValidator : AbstractValidator<ModerateFeedbackRequest>
{
    public ModerateFeedbackRequestValidator()
    {
        RuleFor(x => x.Action).IsInEnum();
    }
}

public sealed class FeedbackSearchQueryValidator : AbstractValidator<FeedbackSearchQuery>
{
    public FeedbackSearchQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5).When(x => x.Rating is not null);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
        RuleFor(x => x.Category).IsInEnum().When(x => x.Category is not null);
        RuleFor(x => x.Sentiment).IsInEnum().When(x => x.Sentiment is not null);
        RuleFor(x => x.Sort).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Sort));
        RuleFor(x => x.Search).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Search));
    }
}

public sealed class ReactionRequestValidator : AbstractValidator<ReactionRequest>
{
    public ReactionRequestValidator()
    {
        RuleFor(x => x.ReactionType).IsInEnum();
    }
}

public sealed class CreateReplyRequestValidator : AbstractValidator<CreateReplyRequest>
{
    public CreateReplyRequestValidator()
    {
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ReplyDecisionRequestValidator : AbstractValidator<ReplyDecisionRequest>
{
    public ReplyDecisionRequestValidator()
    {
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.Reply)
            .NotEmpty()
            .MaximumLength(2000)
            .When(x => x.Decision is ReplyDecision.Edit or ReplyDecision.Save);
    }
}

public sealed class CreateComplaintRequestValidator : AbstractValidator<CreateComplaintRequest>
{
    public CreateComplaintRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.FeedbackId!.Value).NotEmpty().When(x => x.FeedbackId is not null);
        RuleFor(x => x.Priority).IsInEnum().When(x => x.Priority is not null);
    }
}

public sealed class ComplaintStatusUpdateRequestValidator : AbstractValidator<ComplaintStatusUpdateRequest>
{
    public ComplaintStatusUpdateRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.AssignedTo!.Value).NotEmpty().When(x => x.AssignedTo is not null);
    }
}
