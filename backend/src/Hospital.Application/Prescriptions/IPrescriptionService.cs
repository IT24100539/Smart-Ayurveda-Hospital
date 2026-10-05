using Hospital.Application.Common;
using Hospital.Application.Prescriptions.Dtos;

namespace Hospital.Application.Prescriptions;

public interface IPrescriptionService
{
    Task<PagedResult<PrescriptionDto>> ListAsync(
        Guid? patientId,
        Guid? appointmentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<PrescriptionDto>> ListMineAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<PrescriptionDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<PrescriptionHistoryDto> GetHistoryAsync(Guid id, CancellationToken cancellationToken);

    Task<PrescriptionDto> CreateAsync(CreatePrescriptionRequest request, CancellationToken cancellationToken);

    Task<PrescriptionDto> UpdateDraftAsync(Guid id, UpdatePrescriptionRequest request, CancellationToken cancellationToken);

    Task<PrescriptionDto> IssueAsync(Guid id, CancellationToken cancellationToken);

    Task<PrescriptionDto> ReviseAsync(Guid id, RevisePrescriptionRequest request, CancellationToken cancellationToken);

    Task<PrescriptionDto> CancelAsync(Guid id, CancellationToken cancellationToken);
}
