using Hospital.Domain.Entities;

namespace Hospital.Application.Abstractions;

public interface IPatientDeviceTokenRepository
{
    Task<PatientDeviceToken?> FindByTokenAsync(string token, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListTokensForPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task AddAsync(PatientDeviceToken deviceToken, CancellationToken cancellationToken);
}
