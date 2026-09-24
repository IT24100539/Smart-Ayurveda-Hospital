using Hospital.Application.Wards;

namespace Hospital.Application.Wards;

public interface IWardService
{
    Task<IEnumerable<WardOccupancyDto>> GetAllOccupancyAsync(CancellationToken cancellationToken);
    Task<WardOccupancyDto?> GetOccupancyAsync(Guid id, bool forPatient, CancellationToken cancellationToken);
    Task<AdmissionRequestDto> RequestAdmissionAsync(CreateAdmissionRequestRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdmissionRequestDto>> ListPendingAdmissionsAsync(CancellationToken cancellationToken);
    Task<bool> DecideAdmissionAsync(Guid id, AdmissionDecisionRequest request, CancellationToken cancellationToken);
}
