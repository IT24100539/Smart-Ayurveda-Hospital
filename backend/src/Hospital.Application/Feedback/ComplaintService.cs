using Hospital.Application.Abstractions;
using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Communication;

public sealed class ComplaintService : IComplaintService
{
    public static readonly TimeSpan OverdueAfter = TimeSpan.FromDays(5);

    private readonly IComplaintRepository _complaints;
    private readonly IFeedbackRepository _feedback;
    private readonly INotificationRepository _notifications;
    private readonly IStaffUserRepository _staffUsers;
    private readonly IActorContext _actors;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ComplaintService(
        IComplaintRepository complaints,
        IFeedbackRepository feedback,
        INotificationRepository notifications,
        IStaffUserRepository staffUsers,
        IActorContext actors,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _complaints = complaints;
        _feedback = feedback;
        _notifications = notifications;
        _staffUsers = staffUsers;
        _actors = actors;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ComplaintSummaryDto> CreateAsync(CreateComplaintRequest request, CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        if (request.FeedbackId is Guid feedbackId)
        {
            var feedback = await _feedback.GetByIdAsync(feedbackId, cancellationToken)
                ?? throw new NotFoundException(nameof(Feedback), feedbackId);
            if (feedback.PatientId != patient.Id)
            {
                throw new ForbiddenException("You can only attach your own feedback to a complaint.");
            }
        }

        var complaint = new Complaint
        {
            PatientId = patient.Id,
            Patient = patient,
            FeedbackId = request.FeedbackId,
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            Priority = request.Priority ?? ComplaintPriority.Normal,
            Status = ComplaintStatus.Open
        };

        await _complaints.AddAsync(complaint, cancellationToken);
        await AddNotificationAsync(
            complaint,
            "Complaint received",
            "We have received your concern and will review it.",
            NotificationType.ComplaintUpdate,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(complaint);
    }

    public async Task<ComplaintSummaryDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _actors.RequireUserAsync(cancellationToken);
        var complaint = await RequireComplaintAsync(id, cancellationToken);
        if (user.Role == UserRole.Patient)
        {
            var patient = await _actors.RequirePatientAsync(cancellationToken);
            if (complaint.PatientId != patient.Id)
            {
                throw new ForbiddenException("You can only view your own complaints.");
            }
        }
        else
        {
            await _actors.RequireStaffAsync(cancellationToken);
        }

        return Map(complaint);
    }

    public async Task<IReadOnlyList<ComplaintSummaryDto>> ListAsync(
        ComplaintStatus? status,
        ComplaintPriority? priority,
        CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var items = await _complaints.ListAsync(status, priority, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<StaffAssigneeDto>> ListAssigneesAsync(CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var staff = await _staffUsers.ListActiveAsync(cancellationToken);
        return staff
            .Select(item => new StaffAssigneeDto(item.Id, item.FullName, item.Role))
            .ToList();
    }

    public async Task<IReadOnlyList<ComplaintSummaryDto>> ListMineAsync(CancellationToken cancellationToken)
    {
        var patient = await _actors.RequirePatientAsync(cancellationToken);
        var items = await _complaints.ListForPatientAsync(patient.Id, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<ComplaintSummaryDto> UpdateStatusAsync(
        Guid id,
        ComplaintStatusUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var staff = await _actors.RequireStaffAsync(cancellationToken);
        var complaint = await RequireComplaintAsync(id, cancellationToken);
        if (request.AssignedTo is Guid assigneeId)
        {
            await AssignAsync(complaint, assigneeId, cancellationToken);
        }

        if (request.Status == ComplaintStatus.Escalated)
        {
            return await EscalateCoreAsync(complaint, staff, cancellationToken);
        }

        var changed = complaint.Status != request.Status;
        complaint.Status = request.Status;
        if (changed)
        {
            await AddNotificationAsync(
                complaint,
                "Complaint update",
                $"Your complaint \"{Truncate(complaint.Subject, 120)}\" is now {request.Status}.",
                NotificationType.ComplaintUpdate,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(complaint);
    }

    public async Task<ComplaintSummaryDto> EscalateAsync(Guid id, CancellationToken cancellationToken)
    {
        var staff = await _actors.RequireStaffAsync(cancellationToken);
        var complaint = await RequireComplaintAsync(id, cancellationToken);
        return await EscalateCoreAsync(complaint, staff, cancellationToken);
    }

    public async Task<IReadOnlyList<ComplaintSummaryDto>> GetOverdueComplaintsAsync(CancellationToken cancellationToken)
    {
        await _actors.RequireStaffAsync(cancellationToken);
        var cutoff = _clock.UtcNow.Subtract(OverdueAfter);
        var items = await _complaints.ListOpenOlderThanAsync(cutoff, cancellationToken);
        return items.Select(Map).ToList();
    }

    private async Task<ComplaintSummaryDto> EscalateCoreAsync(
        Complaint complaint,
        StaffUser staff,
        CancellationToken cancellationToken)
    {
        var alreadyEscalated = complaint.Status == ComplaintStatus.Escalated;
        complaint.Status = ComplaintStatus.Escalated;
        complaint.Priority = ComplaintPriority.High;
        complaint.EscalatedAt ??= _clock.UtcNow;
        complaint.AssignedTo ??= staff.Id;
        if (!alreadyEscalated)
        {
            await AddNotificationAsync(
                complaint,
                "Complaint escalated",
                $"Your complaint \"{Truncate(complaint.Subject, 120)}\" has been escalated for hospital review.",
                NotificationType.ComplaintEscalated,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(complaint);
    }

    private async Task<Complaint> RequireComplaintAsync(Guid id, CancellationToken cancellationToken) =>
        await _complaints.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Complaint), id);

    private async Task AssignAsync(Complaint complaint, Guid assigneeId, CancellationToken cancellationToken)
    {
        var assignee = await _staffUsers.GetByIdAsync(assigneeId, cancellationToken)
            ?? throw new NotFoundException("Staff", assigneeId);
        complaint.AssignedTo = assignee.Id;
    }

    private async Task AddNotificationAsync(
        Complaint complaint,
        string title,
        string message,
        NotificationType type,
        CancellationToken cancellationToken)
    {
        await _notifications.AddAsync(new Notification
        {
            PatientId = complaint.PatientId,
            Title = title,
            Message = Truncate(message, 1000),
            Type = type,
            IsRead = false
        }, cancellationToken);
    }

    private ComplaintSummaryDto Map(Complaint complaint)
    {
        var patientName = complaint.Patient is null
            ? "Patient"
            : $"{complaint.Patient.FirstName} {complaint.Patient.LastName}".Trim();
        var overdue = complaint.Status == ComplaintStatus.Open
            && complaint.CreatedAt < _clock.UtcNow.Subtract(OverdueAfter);

        return new ComplaintSummaryDto(
            complaint.Id,
            complaint.PatientId,
            patientName,
            complaint.FeedbackId,
            complaint.Subject,
            complaint.Description,
            complaint.Priority,
            complaint.Status,
            complaint.AssignedTo,
            complaint.EscalatedAt,
            complaint.CreatedAt,
            overdue);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
