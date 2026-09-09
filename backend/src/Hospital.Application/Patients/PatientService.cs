using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Application.Patients.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Patients;

public sealed class PatientService : IPatientService
{
    private readonly IPatientRepository _patients;
    private readonly IUhidGenerator _uhid;
    private readonly IUnitOfWork _unitOfWork;

    public PatientService(IPatientRepository patients, IUhidGenerator uhid, IUnitOfWork unitOfWork)
    {
        _patients = patients;
        _uhid = uhid;
        _unitOfWork = unitOfWork;
    }

    public async Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        var existing = await _patients.GetByPhoneAsync(request.Phone.Trim(), cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A patient with phone '{request.Phone}' already exists ({existing.Uhid}).");
        }

        var patient = new Patient
        {
            Uhid = await _uhid.NextAsync(cancellationToken),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Phone = request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = request.Address,
            BloodGroup = request.BloodGroup,
            Allergies = request.Allergies,
            Prakriti = request.Prakriti,
            Vikriti = request.Vikriti
        };

        await _patients.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(patient);
    }

    public async Task<PatientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), id);
        return Map(patient);
    }

    public async Task<PagedResult<PatientDto>> SearchAsync(string? query, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _patients.SearchAsync(query, page, pageSize, cancellationToken);
        return new PagedResult<PatientDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PatientDto> UpdateAsync(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Patient), id);

        var byPhone = await _patients.GetByPhoneAsync(request.Phone.Trim(), cancellationToken);
        if (byPhone is not null && byPhone.Id != id)
        {
            throw new ConflictException($"A patient with phone '{request.Phone}' already exists ({byPhone.Uhid}).");
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.Phone = request.Phone.Trim();
        patient.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        patient.Address = request.Address;
        patient.BloodGroup = request.BloodGroup;
        patient.Allergies = request.Allergies;
        patient.Prakriti = request.Prakriti;
        patient.Vikriti = request.Vikriti;
        patient.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(patient);
    }

    private static PatientDto Map(Patient p) => new(
        p.Id,
        p.Uhid,
        p.FirstName,
        p.LastName,
        p.DateOfBirth,
        p.Gender,
        p.Phone,
        p.Email,
        p.Address,
        p.BloodGroup,
        p.Allergies,
        p.Prakriti,
        p.Vikriti,
        p.IsActive,
        p.CreatedAt);
}
