using FluentValidation;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;

namespace Hospital.Application.Communication.Validators;

public sealed class RegisterDeviceTokenRequestValidator : AbstractValidator<RegisterDeviceTokenRequest>
{
    public RegisterDeviceTokenRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(PatientDeviceToken.TokenMaxLength);
        RuleFor(x => x.Platform)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(platform => PatientDeviceToken.IsKnownPlatform(platform.Trim().ToLowerInvariant()))
            .WithMessage("Platform must be android, ios, or web.");
    }
}
