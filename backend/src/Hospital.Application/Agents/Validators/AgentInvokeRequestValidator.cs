using FluentValidation;
using Hospital.Application.Agents.Dtos;

namespace Hospital.Application.Agents.Validators;

public sealed class StartAgentWorkflowRequestValidator : AbstractValidator<StartAgentWorkflowRequest>
{
    public StartAgentWorkflowRequestValidator()
    {
        RuleFor(x => x.Objective ?? x.ObjectiveText).NotEmpty().MaximumLength(4000);
    }
}

public sealed class ApproveAgentWorkflowRequestValidator : AbstractValidator<ApproveAgentWorkflowRequest>
{
    public ApproveAgentWorkflowRequestValidator()
    {
        RuleFor(x => x.Reply).MaximumLength(2000).When(x => !string.IsNullOrWhiteSpace(x.Reply));
    }
}

public sealed class AgentInvokeRequestValidator : AbstractValidator<AgentInvokeRequest>
{
    public AgentInvokeRequestValidator()
    {
        RuleFor(x => x.Agent).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Prompt).NotEmpty().MaximumLength(8000);
    }
}
