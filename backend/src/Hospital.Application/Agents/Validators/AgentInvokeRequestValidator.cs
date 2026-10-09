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

public sealed class AskPatientInfoRequestValidator : AbstractValidator<AskPatientInfoRequest>
{
    public AskPatientInfoRequestValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AskTreatmentInfoRequestValidator : AbstractValidator<AskTreatmentInfoRequest>
{
    public AskTreatmentInfoRequestValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
    }
}

public sealed class AskCharakaRequestValidator : AbstractValidator<AskCharakaRequest>
{
    public AskCharakaRequestValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.History).Must(history => history is null || history.Count <= 12);
        RuleForEach(x => x.History).ChildRules(turn =>
        {
            turn.RuleFor(item => item.Role).NotEmpty().MaximumLength(20);
            turn.RuleFor(item => item.Text).NotEmpty().MaximumLength(2000);
        });
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
