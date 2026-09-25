using System.Text.Json;
using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Application.Workflows.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Workflows;

public sealed class WorkflowExecutionService : IWorkflowExecutionService
{
    private readonly IWorkflowExecutionRepository _executions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IActorContext _actors;

    public WorkflowExecutionService(
        IWorkflowExecutionRepository executions,
        IUnitOfWork unitOfWork,
        IActorContext actors)
    {
        _executions = executions;
        _unitOfWork = unitOfWork;
        _actors = actors;
    }

    public async Task<WorkflowExecutionDto> UpsertAsync(UpsertWorkflowExecutionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _executions.GetByIdAsync(request.Id, cancellationToken);
        if (existing is null)
        {
            existing = new WorkflowExecution { Id = request.Id };
            Apply(existing, request);
            await _executions.AddAsync(existing, cancellationToken);
        }
        else
        {
            Apply(existing, request);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(existing);
    }

    public async Task<WorkflowExecutionDto> UpdateAsync(Guid id, UpdateWorkflowExecutionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _executions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowExecution), id);
        Apply(existing, request);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(existing);
    }

    public async Task<WorkflowExecutionDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var existing = await _executions.GetByIdAsync(id, cancellationToken);
        return existing is null ? null : ToDto(existing);
    }

    public async Task<PagedResult<WorkflowExecutionDto>> SearchAsync(WorkflowExecutionSearchQuery query, CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var (items, total) = await _executions.SearchAsync(
            query.AgentName,
            query.ApprovalStatus,
            query.From,
            query.To,
            query.Search,
            query.Sort,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<WorkflowExecutionDto>
        {
            Items = items.Select(ToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static void Apply(WorkflowExecution entity, UpsertWorkflowExecutionRequest request)
    {
        entity.AgentName = request.AgentName.Trim();
        entity.ObjectiveText = request.ObjectiveText.Trim();
        entity.PlanJson = request.PlanJson.GetRawText();
        entity.CompletedStepsJson = request.CompletedStepsJson.GetRawText();
        entity.ToolResultsJson = request.ToolResultsJson.GetRawText();
        entity.ValidationResultsJson = request.ValidationResultsJson.GetRawText();
        entity.ErrorsJson = request.ErrorsJson is { ValueKind: JsonValueKind.Null } ? null : request.ErrorsJson?.GetRawText();
        entity.ApprovalStatus = request.ApprovalStatus;
        entity.FinalOutcome = request.FinalOutcome;
        entity.RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType) ? null : request.RelatedEntityType.Trim();
        entity.RelatedEntityId = request.RelatedEntityId;
    }

    private static void Apply(WorkflowExecution entity, UpdateWorkflowExecutionRequest request)
    {
        entity.AgentName = request.AgentName.Trim();
        entity.ObjectiveText = request.ObjectiveText.Trim();
        entity.PlanJson = request.PlanJson.GetRawText();
        entity.CompletedStepsJson = request.CompletedStepsJson.GetRawText();
        entity.ToolResultsJson = request.ToolResultsJson.GetRawText();
        entity.ValidationResultsJson = request.ValidationResultsJson.GetRawText();
        entity.ErrorsJson = request.ErrorsJson is { ValueKind: JsonValueKind.Null } ? null : request.ErrorsJson?.GetRawText();
        entity.ApprovalStatus = request.ApprovalStatus;
        entity.FinalOutcome = request.FinalOutcome;
        entity.RelatedEntityType = string.IsNullOrWhiteSpace(request.RelatedEntityType) ? null : request.RelatedEntityType.Trim();
        entity.RelatedEntityId = request.RelatedEntityId;
    }

    internal static WorkflowExecutionDto ToDto(WorkflowExecution entity) => new()
    {
        Id = entity.Id,
        AgentName = entity.AgentName,
        ObjectiveText = entity.ObjectiveText,
        Plan = Parse(entity.PlanJson, "[]"),
        CompletedSteps = Parse(entity.CompletedStepsJson, "[]"),
        ToolResults = Parse(entity.ToolResultsJson, "[]"),
        ValidationResults = Parse(entity.ValidationResultsJson, "[]"),
        Errors = string.IsNullOrWhiteSpace(entity.ErrorsJson) ? null : Parse(entity.ErrorsJson, "[]"),
        ApprovalStatus = entity.ApprovalStatus,
        FinalOutcome = entity.FinalOutcome,
        RelatedEntityType = entity.RelatedEntityType,
        RelatedEntityId = entity.RelatedEntityId,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static JsonElement Parse(string? json, string fallback)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? fallback : json);
        return document.RootElement.Clone();
    }
}
