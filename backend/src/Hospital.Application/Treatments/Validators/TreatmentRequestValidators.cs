using FluentValidation;
using Hospital.Application.Treatments.Dtos;

namespace Hospital.Application.Treatments.Validators;

public sealed class TreatmentSearchQueryValidator : AbstractValidator<TreatmentSearchQuery>
{
    public TreatmentSearchQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Category).IsInEnum().When(x => x.Category is not null);
        RuleFor(x => x.Name).MaximumLength(160);
        RuleFor(x => x.Sort).MaximumLength(40);
    }
}

public sealed class CreateTreatmentRequestValidator : AbstractValidator<CreateTreatmentRequest>
{
    public CreateTreatmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.NameSinhala).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DescriptionSinhala).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 24 * 60);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Schedules).SetValidator(new CreateScheduleEntryRequestValidator())
            .When(x => x.Schedules is not null);
    }
}

public sealed class UpdateTreatmentRequestValidator : AbstractValidator<UpdateTreatmentRequest>
{
    public UpdateTreatmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.NameSinhala).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.DescriptionSinhala).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(5, 24 * 60);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateScheduleEntryRequestValidator : AbstractValidator<CreateScheduleEntryRequest>
{
    public CreateScheduleEntryRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.MaxSlotsPerDay).InclusiveBetween(1, 200);
    }
}

public sealed class UpdateScheduleEntryRequestValidator : AbstractValidator<UpdateScheduleEntryRequest>
{
    public UpdateScheduleEntryRequestValidator()
    {
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.MaxSlotsPerDay).InclusiveBetween(1, 200);
    }
}
