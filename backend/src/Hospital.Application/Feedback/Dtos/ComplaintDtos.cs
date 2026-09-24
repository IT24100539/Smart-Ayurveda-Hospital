using Hospital.Domain.Enums;

namespace Hospital.Application.Communication.Dtos;

public sealed record CreateComplaintRequest(
    string Subject,
    string Description,
    Guid? FeedbackId,
    ComplaintPriority? Priority);

public sealed record ComplaintStatusUpdateRequest(
    ComplaintStatus Status,
    Guid? AssignedTo);

public sealed record StaffAssigneeDto(Guid Id, string FullName, StaffRole Role);

public sealed record ComplaintSummaryDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid? FeedbackId,
    string Subject,
    string Description,
    ComplaintPriority Priority,
    ComplaintStatus Status,
    Guid? AssignedTo,
    DateTimeOffset? EscalatedAt,
    DateTimeOffset CreatedAt,
    bool IsOverdue);
