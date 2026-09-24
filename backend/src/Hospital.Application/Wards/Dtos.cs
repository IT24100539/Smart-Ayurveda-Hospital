using Hospital.Domain.Enums;

namespace Hospital.Application.Wards;

public sealed record WardOccupancyDto(
    Guid Id,
    string Name,
    string NameSinhala,
    WardGender Gender,
    int TotalCapacity,
    int OccupiedBeds,
    IEnumerable<BedDto> Beds);

public sealed record BedDto(Guid Id, string BedLabel, bool IsOccupied);

public sealed record AdmissionRequestDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid? WardId,
    Guid? BedId,
    string Reason,
    DateOnly PreferredDate,
    string Status,
    bool RequestedByAgent,
    Guid? DecidedBy,
    DateTimeOffset? DecidedAt);

public sealed record CreateAdmissionRequestRequest(Guid PatientId, Guid? WardId, string Reason, DateOnly PreferredDate, bool RequestedByAgent);

public sealed record AdmissionDecisionRequest(bool Approve, Guid DecidedBy);
