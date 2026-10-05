using FluentValidation;
using Hospital.Application.Common;
using Hospital.Application.Wards;

namespace Hospital.Application.Wards.Validators;

public sealed class CreateAdmissionRequestRequestValidator : AbstractValidator<CreateAdmissionRequestRequest>
{
    public CreateAdmissionRequestRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.PreferredDate).Must(DateSanity.IsCalendarDate)
            .WithMessage("Preferred date is outside the supported range.");
        RuleFor(x => x.WardId!.Value).NotEmpty().When(x => x.WardId is not null);
    }
}

public sealed class AdmissionDecisionRequestValidator : AbstractValidator<AdmissionDecisionRequest>
{
    public AdmissionDecisionRequestValidator()
    {
        RuleFor(x => x.DecidedBy).NotEmpty();
    }
}
