using Hospital.Application.Common;
using Hospital.Application.Doctors.Dtos;

namespace Hospital.Application.Doctors;

public interface IDoctorService
{
    Task<PagedResult<DoctorDto>> ListAsync(string? query, bool? activeOnly, int page, int pageSize, CancellationToken cancellationToken);
    Task<DoctorDto> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<DoctorDto> CreateAsync(CreateDoctorRequest request, CancellationToken cancellationToken);
    Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorRequest request, CancellationToken cancellationToken);
    Task<DoctorDto> DeactivateAsync(Guid id, CancellationToken cancellationToken);
    Task<DoctorDto> SavePhotoAsync(Guid id, DoctorPhotoContent photo, CancellationToken cancellationToken);
    Task<DoctorDto> RemovePhotoAsync(Guid id, CancellationToken cancellationToken);
    Task<DoctorPhotoFile> OpenPhotoAsync(Guid id, CancellationToken cancellationToken);
}
