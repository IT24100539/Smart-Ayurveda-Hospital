using Hospital.Domain.Enums;

namespace Hospital.Application.Treatments.Dtos;

public sealed class TreatmentSearchQuery
{
    public string? Name { get; init; }
    public TreatmentCategory? Category { get; init; }
    public bool? ActiveOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Sort { get; init; }
}

public sealed record CreateTreatmentRequest(
    string Name,
    string NameSinhala,
    string Description,
    string DescriptionSinhala,
    TreatmentCategory Category,
    int DurationMinutes,
    decimal UnitPrice,
    IReadOnlyList<CreateScheduleEntryRequest>? Schedules);

public sealed record UpdateTreatmentRequest(
    string Name,
    string NameSinhala,
    string Description,
    string DescriptionSinhala,
    TreatmentCategory Category,
    int DurationMinutes,
    decimal UnitPrice);

public sealed record CreateScheduleEntryRequest(
    Weekday DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxSlotsPerDay,
    Guid? TherapistId,
    bool IsActive = true);

public sealed record UpdateScheduleEntryRequest(
    Weekday DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxSlotsPerDay,
    Guid? TherapistId,
    bool IsActive);

public sealed record TreatmentSummaryDto(
    Guid Id,
    string Name,
    string NameSinhala,
    string Description,
    string DescriptionSinhala,
    TreatmentCategory Category,
    int DurationMinutes,
    decimal UnitPrice,
    bool IsActive,
    IReadOnlyList<DayOfWeek> AvailableDays);

public sealed record TreatmentDetailDto(
    Guid Id,
    string Name,
    string NameSinhala,
    string Description,
    string DescriptionSinhala,
    TreatmentCategory Category,
    int DurationMinutes,
    decimal UnitPrice,
    bool IsActive,
    IReadOnlyList<DayOfWeek> AvailableDays,
    IReadOnlyList<ScheduleEntryDto> Schedule);

public sealed record ScheduleEntryDto(
    Guid Id,
    Guid TreatmentId,
    Guid? TherapistId,
    string? TherapistName,
    Weekday DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int MaxSlotsPerDay,
    bool IsActive);

public sealed record TreatmentAvailabilityDto(
    Guid TreatmentId,
    DateOnly Date,
    DayOfWeek DayOfWeek,
    bool IsAvailable,
    int MaxSlotsPerDay,
    int BookedCount,
    int RemainingSlots);
