using System.Text.Json;
using FluentValidation;
using Hospital.Application.Workflows.Dtos;

namespace Hospital.Application.Workflows.Validators;

public sealed class UpsertWorkflowExecutionRequestValidator : AbstractValidator<UpsertWorkflowExecutionRequest>
{
    public UpsertWorkflowExecutionRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.AgentName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ObjectiveText).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.PlanJson).Must(BeArray).WithMessage("PlanJson must be a JSON array.");
        RuleFor(x => x.CompletedStepsJson).Must(BeArray).WithMessage("CompletedStepsJson must be a JSON array.");
        RuleFor(x => x.ToolResultsJson).Must(BeArray).WithMessage("ToolResultsJson must be a JSON array.");
        RuleFor(x => x.ValidationResultsJson).Must(BeArray).WithMessage("ValidationResultsJson must be a JSON array.");
        RuleFor(x => x.ErrorsJson).Must(BeArrayOrNull).WithMessage("ErrorsJson must be a JSON array or null.");
        RuleFor(x => x.ApprovalStatus).IsInEnum();
        RuleFor(x => x.FinalOutcome).IsInEnum();
        RuleFor(x => x.RelatedEntityType).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.RelatedEntityType));
        RuleFor(x => x.RelatedEntityId).NotEmpty().When(x => x.RelatedEntityId is not null);
    }

    private static bool BeArray(JsonElement element) => element.ValueKind == JsonValueKind.Array;

    private static bool BeArrayOrNull(JsonElement? element) =>
        element is null || element.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Null;
}

public sealed class UpdateWorkflowExecutionRequestValidator : AbstractValidator<UpdateWorkflowExecutionRequest>
{
    public UpdateWorkflowExecutionRequestValidator()
    {
        RuleFor(x => x.AgentName).NotEmpty().MaximumLength(64);
        RuleFor(x => x.ObjectiveText).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.PlanJson).Must(element => element.ValueKind == JsonValueKind.Array);
        RuleFor(x => x.CompletedStepsJson).Must(element => element.ValueKind == JsonValueKind.Array);
        RuleFor(x => x.ToolResultsJson).Must(element => element.ValueKind == JsonValueKind.Array);
        RuleFor(x => x.ValidationResultsJson).Must(element => element.ValueKind == JsonValueKind.Array);
        RuleFor(x => x.ErrorsJson).Must(element =>
            element is null || element.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Null);
        RuleFor(x => x.ApprovalStatus).IsInEnum();
        RuleFor(x => x.FinalOutcome).IsInEnum();
        RuleFor(x => x.RelatedEntityType).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.RelatedEntityType));
        RuleFor(x => x.RelatedEntityId).NotEmpty().When(x => x.RelatedEntityId is not null);
    }
}

public sealed class WorkflowExecutionSearchQueryValidator : AbstractValidator<WorkflowExecutionSearchQuery>
{
    public WorkflowExecutionSearchQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.AgentName).MaximumLength(64).When(x => !string.IsNullOrWhiteSpace(x.AgentName));
        RuleFor(x => x.ApprovalStatus).IsInEnum().When(x => x.ApprovalStatus is not null);
        RuleFor(x => x.Search).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.Search));
        RuleFor(x => x.Sort).MaximumLength(32).When(x => !string.IsNullOrWhiteSpace(x.Sort));
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From!.Value).When(x => x.From is not null && x.To is not null);
    }
}
