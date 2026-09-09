using Hospital.Domain.Enums;

namespace Hospital.Application.Patients.Dtos;

public sealed record CreatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string Phone,
    string? Email,
    string? Address,
    string? BloodGroup,
    string? Allergies,
    DoshaType Prakriti,
    DoshaType Vikriti);

public sealed record UpdatePatientRequest(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string Phone,
    string? Email,
    string? Address,
    string? BloodGroup,
    string? Allergies,
    DoshaType Prakriti,
    DoshaType Vikriti,
    bool IsActive);

public sealed record PatientDto(
    Guid Id,
    string Uhid,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string Phone,
    string? Email,
    string? Address,
    string? BloodGroup,
    string? Allergies,
    DoshaType Prakriti,
    DoshaType Vikriti,
    bool IsActive,
    DateTimeOffset CreatedAt);
