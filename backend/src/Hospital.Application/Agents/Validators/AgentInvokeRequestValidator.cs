using FluentValidation;
using Hospital.Application.Agents.Dtos;

namespace Hospital.Application.Agents.Validators;

public sealed class AgentInvokeRequestValidator : AbstractValidator<AgentInvokeRequest>
{
    public AgentInvokeRequestValidator()
    {
        RuleFor(x => x.Agent).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(8000);
    }
}
