using Hospital.Application.Communication.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.Communication;

public interface IComplaintService
{
    Task<ComplaintSummaryDto> CreateAsync(CreateComplaintRequest request, CancellationToken cancellationToken);
    Task<ComplaintSummaryDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ComplaintSummaryDto>> ListAsync(
        ComplaintStatus? status,
        ComplaintPriority? priority,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<StaffAssigneeDto>> ListAssigneesAsync(CancellationToken cancellationToken);

    /// <summary>Complaints raised by the signed-in patient.</summary>
    Task<IReadOnlyList<ComplaintSummaryDto>> ListMineAsync(CancellationToken cancellationToken);
    Task<ComplaintSummaryDto> UpdateStatusAsync(Guid id, ComplaintStatusUpdateRequest request, CancellationToken cancellationToken);
    Task<ComplaintSummaryDto> EscalateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Complaints still Open more than 5 days after CreatedAt.
    /// The API host escalates these on a timer by calling <see cref="EscalateAsync"/>.
    /// </summary>
    Task<IReadOnlyList<ComplaintSummaryDto>> GetOverdueComplaintsAsync(CancellationToken cancellationToken);
}
