using Hospital.Application.Common;
using Hospital.Application.Patients.Dtos;

namespace Hospital.Application.Patients;

public interface IPatientService
{
    Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken);
    Task<PatientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<PatientDto>> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken);
    Task<PatientDto> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken);
}
