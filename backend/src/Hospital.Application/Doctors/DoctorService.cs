using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Application.Doctors.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Doctors;

public sealed class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctors;
    private readonly IDoctorPhotoStore _photos;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _current;

    public DoctorService(
        IDoctorRepository doctors,
        IDoctorPhotoStore photos,
        IUnitOfWork unitOfWork,
        ICurrentUser current)
    {
        _doctors = doctors;
        _photos = photos;
        _unitOfWork = unitOfWork;
        _current = current;
    }

    public async Task<PagedResult<DoctorDto>> ListAsync(
        string? query,
        bool? activeOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureReader();
        var onlyActive = _current.Role != UserRole.Admin || activeOnly == true;
        var (items, total) = await _doctors.ListAsync(query, onlyActive, page, pageSize, cancellationToken);
        var ratings = await _doctors.GetRatingsAsync(items.Select(x => x.Id).ToArray(), cancellationToken);
        return new PagedResult<DoctorDto>
        {
            Items = items.Select(doctor => Map(doctor, ratings)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DoctorDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await RequireVisibleAsync(id, cancellationToken);
        var ratings = await _doctors.GetRatingsAsync(new[] { doctor.Id }, cancellationToken);
        return Map(doctor, ratings);
    }

    public async Task<DoctorDto> CreateAsync(CreateDoctorRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var doctor = new Doctor
        {
            Name = request.Name.Trim(),
            Specialty = request.Specialty.Trim(),
            Qualifications = request.Qualifications.Trim(),
            Bio = CleanBio(request.Bio),
            IsActive = true,
            IsSample = false
        };
        await _doctors.AddAsync(doctor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(doctor, EmptyRatings);
    }

    public async Task<DoctorDto> UpdateAsync(Guid id, UpdateDoctorRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var doctor = await RequireAsync(id, cancellationToken);
        doctor.Name = request.Name.Trim();
        doctor.Specialty = request.Specialty.Trim();
        doctor.Qualifications = request.Qualifications.Trim();
        doctor.Bio = CleanBio(request.Bio);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var ratings = await _doctors.GetRatingsAsync(new[] { doctor.Id }, cancellationToken);
        return Map(doctor, ratings);
    }

    public async Task<DoctorDto> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var doctor = await RequireAsync(id, cancellationToken);
        doctor.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var ratings = await _doctors.GetRatingsAsync(new[] { doctor.Id }, cancellationToken);
        return Map(doctor, ratings);
    }

    public async Task<DoctorDto> SavePhotoAsync(Guid id, DoctorPhotoContent photo, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var doctor = await RequireAsync(id, cancellationToken);
        var previousKey = doctor.PhotoStorageKey;
        var saved = await _photos.SaveAsync(doctor.Id, photo.Content, photo.ContentType, cancellationToken);
        if (!string.IsNullOrEmpty(previousKey) && previousKey != saved.StorageKey)
        {
            await _photos.DeleteAsync(previousKey, cancellationToken);
        }

        doctor.PhotoStorageKey = saved.StorageKey;
        doctor.PhotoContentType = saved.ContentType;
        doctor.PhotoSizeBytes = saved.SizeBytes;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var ratings = await _doctors.GetRatingsAsync(new[] { doctor.Id }, cancellationToken);
        return Map(doctor, ratings);
    }

    public async Task<DoctorDto> RemovePhotoAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureAdmin();
        var doctor = await RequireAsync(id, cancellationToken);
        await _photos.DeleteAsync(doctor.PhotoStorageKey, cancellationToken);
        doctor.PhotoStorageKey = null;
        doctor.PhotoContentType = null;
        doctor.PhotoSizeBytes = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var ratings = await _doctors.GetRatingsAsync(new[] { doctor.Id }, cancellationToken);
        return Map(doctor, ratings);
    }

    public async Task<DoctorPhotoFile> OpenPhotoAsync(Guid id, CancellationToken cancellationToken)
    {
        var doctor = await RequireVisibleAsync(id, cancellationToken);
        if (string.IsNullOrEmpty(doctor.PhotoStorageKey)
            || !IsAllowedContentType(doctor.PhotoContentType))
        {
            throw new NotFoundException("Doctor photo", id);
        }

        var stream = _photos.OpenRead(doctor.PhotoStorageKey)
            ?? throw new NotFoundException("Doctor photo", id);
        return new DoctorPhotoFile
        {
            Content = stream,
            ContentType = doctor.PhotoContentType!
        };
    }

    private async Task<Doctor> RequireVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureReader();
        var doctor = await RequireAsync(id, cancellationToken);
        if (_current.Role == UserRole.Patient && !doctor.IsActive)
        {
            throw new NotFoundException(nameof(Doctor), id);
        }

        return doctor;
    }

    private async Task<Doctor> RequireAsync(Guid id, CancellationToken cancellationToken) =>
        await _doctors.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(Doctor), id);

    private void EnsureReader()
    {
        if (_current.Role is not (UserRole.Patient or UserRole.Admin))
        {
            throw new ForbiddenException("You are not allowed to view physicians.");
        }
    }

    private void EnsureAdmin()
    {
        if (_current.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Only an administrator can manage physicians.");
        }
    }

    private static readonly IReadOnlyDictionary<Guid, DoctorRatingSummary> EmptyRatings =
        new Dictionary<Guid, DoctorRatingSummary>();

    private static DoctorDto Map(Doctor doctor, IReadOnlyDictionary<Guid, DoctorRatingSummary> ratings)
    {
        ratings.TryGetValue(doctor.Id, out var rating);
        var hasPhoto = !string.IsNullOrEmpty(doctor.PhotoStorageKey);
        return new DoctorDto
        {
            Id = doctor.Id,
            Name = doctor.Name,
            Specialty = doctor.Specialty,
            Qualifications = doctor.Qualifications,
            Bio = doctor.Bio,
            IsActive = doctor.IsActive,
            IsSample = doctor.IsSample,
            HasPhoto = hasPhoto,
            PhotoUrl = hasPhoto ? $"/api/doctors/{doctor.Id}/photo" : null,
            Rating = rating is null ? null : rating.Average,
            RatingCount = rating is null ? null : rating.Count
        };
    }

    private static string? CleanBio(string? bio) =>
        string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();

    private static bool IsAllowedContentType(string? contentType) =>
        contentType is "image/jpeg" or "image/png" or "image/webp";
}
