using Hospital.Application.Common;
using Hospital.Application.Treatments.Dtos;

namespace Hospital.Application.Treatments;

public interface ITreatmentService
{
    Task<PagedResult<TreatmentSummaryDto>> SearchAsync(TreatmentSearchQuery query, CancellationToken cancellationToken);
    Task<TreatmentDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TreatmentDetailDto> CreateAsync(CreateTreatmentRequest request, CancellationToken cancellationToken);
    Task<TreatmentDetailDto> UpdateAsync(Guid id, UpdateTreatmentRequest request, CancellationToken cancellationToken);
    Task<TreatmentDetailDto> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<TreatmentAvailabilityDto> IsAvailableOnAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// Reusable booking gate for Member 3. Throws <see cref="Hospital.Domain.Exceptions.InvalidScheduleException"/>
    /// when the treatment is inactive or has no active schedule entry for <paramref name="date"/>'s weekday.
    /// </summary>
    Task ValidateBookingDateAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken);

    Task<ScheduleEntryDto> AddScheduleEntryAsync(Guid treatmentId, CreateScheduleEntryRequest request, CancellationToken cancellationToken);
    Task<ScheduleEntryDto> UpdateScheduleEntryAsync(Guid treatmentId, Guid entryId, UpdateScheduleEntryRequest request, CancellationToken cancellationToken);
    Task RemoveScheduleEntryAsync(Guid treatmentId, Guid entryId, CancellationToken cancellationToken);
}
