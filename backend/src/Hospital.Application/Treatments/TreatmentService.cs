using Hospital.Application.Abstractions;
using Hospital.Application.Common;
using Hospital.Application.Treatments.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Treatments;

public sealed class TreatmentService : ITreatmentService
{
    private readonly ITreatmentRepository _treatments;
    private readonly IAppointmentCountProvider _appointmentCounts;
    private readonly IUnitOfWork _unitOfWork;

    public TreatmentService(
        ITreatmentRepository treatments,
        IAppointmentCountProvider appointmentCounts,
        IUnitOfWork unitOfWork)
    {
        _treatments = treatments;
        _appointmentCounts = appointmentCounts;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<TreatmentSummaryDto>> SearchAsync(
        TreatmentSearchQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var activeOnly = query.ActiveOnly ?? true;
        var (items, total) = await _treatments.SearchAsync(
            query.Name,
            query.Category,
            activeOnly,
            page,
            pageSize,
            query.Sort,
            cancellationToken);

        return new PagedResult<TreatmentSummaryDto>
        {
            Items = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TreatmentDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), id);
        return MapDetail(treatment);
    }

    public async Task<TreatmentDetailDto> CreateAsync(CreateTreatmentRequest request, CancellationToken cancellationToken)
    {
        var treatment = new Treatment
        {
            Name = request.Name.Trim(),
            NameSinhala = request.NameSinhala.Trim(),
            Description = request.Description.Trim(),
            DescriptionSinhala = request.DescriptionSinhala.Trim(),
            Category = request.Category,
            DurationMinutes = request.DurationMinutes,
            UnitPrice = request.UnitPrice,
            IsActive = true
        };

        if (request.Schedules is { Count: > 0 })
        {
            foreach (var entry in request.Schedules)
            {
                EnsureUniqueSlot(treatment, entry.TherapistId, entry.DayOfWeek, entry.StartTime, excludeEntryId: null);
                treatment.Schedules.Add(await BuildScheduleAsync(treatment, entry, cancellationToken));
            }
        }

        await _treatments.AddAsync(treatment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await _treatments.GetByIdWithScheduleAsync(treatment.Id, cancellationToken)
            ?? treatment;
        return MapDetail(created);
    }

    public async Task<TreatmentDetailDto> UpdateAsync(Guid id, UpdateTreatmentRequest request, CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), id);

        treatment.Name = request.Name.Trim();
        treatment.NameSinhala = request.NameSinhala.Trim();
        treatment.Description = request.Description.Trim();
        treatment.DescriptionSinhala = request.DescriptionSinhala.Trim();
        treatment.Category = request.Category;
        treatment.DurationMinutes = request.DurationMinutes;
        treatment.UnitPrice = request.UnitPrice;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetail(treatment);
    }

    public async Task<TreatmentDetailDto> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), id);

        treatment.IsActive = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetail(treatment);
    }

    public async Task<TreatmentAvailabilityDto> IsAvailableOnAsync(
        Guid treatmentId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(treatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), treatmentId);

        var weekday = ToWeekday(date.DayOfWeek);
        var matching = treatment.Schedules
            .Where(s => s.IsActive && s.DayOfWeek == weekday)
            .ToList();

        var hasSchedule = treatment.IsActive && matching.Count > 0;
        var maxSlots = matching.Sum(s => s.MaxSlotsPerDay);

        // TODO(Member 3): IAppointmentCountProvider should count appointments for this treatment/date.
        var booked = hasSchedule
            ? await _appointmentCounts.CountBookedSlotsAsync(treatmentId, date, cancellationToken)
            : 0;
        booked = Math.Max(0, booked);
        var remaining = hasSchedule ? Math.Max(0, maxSlots - booked) : 0;

        return new TreatmentAvailabilityDto(
            treatment.Id,
            date,
            date.DayOfWeek,
            IsAvailable: hasSchedule,
            MaxSlotsPerDay: maxSlots,
            BookedCount: booked,
            RemainingSlots: remaining);
    }

    public async Task ValidateBookingDateAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(treatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), treatmentId);

        if (!treatment.IsActive)
        {
            throw new InvalidScheduleException($"Treatment '{treatmentId}' is not active and cannot be booked.");
        }

        var weekday = ToWeekday(date.DayOfWeek);
        var hasSlot = treatment.Schedules.Any(s => s.IsActive && s.DayOfWeek == weekday);
        if (!hasSlot)
        {
            throw new InvalidScheduleException(treatmentId, date);
        }
    }

    public async Task<ScheduleEntryDto> AddScheduleEntryAsync(
        Guid treatmentId,
        CreateScheduleEntryRequest request,
        CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(treatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), treatmentId);

        EnsureUniqueSlot(treatment, request.TherapistId, request.DayOfWeek, request.StartTime, excludeEntryId: null);

        var entry = await BuildScheduleAsync(treatment, request, cancellationToken);
        await _treatments.AddScheduleAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapSchedule(entry);
    }

    public async Task<ScheduleEntryDto> UpdateScheduleEntryAsync(
        Guid treatmentId,
        Guid entryId,
        UpdateScheduleEntryRequest request,
        CancellationToken cancellationToken)
    {
        var treatment = await _treatments.GetByIdWithScheduleAsync(treatmentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Treatment), treatmentId);

        var entry = treatment.Schedules.FirstOrDefault(s => s.Id == entryId)
            ?? throw new NotFoundException(nameof(TreatmentSchedule), entryId);

        EnsureUniqueSlot(treatment, request.TherapistId, request.DayOfWeek, request.StartTime, entryId);

        if (request.TherapistId is { } therapistId)
        {
            var therapist = await _treatments.GetTherapistByIdAsync(therapistId, cancellationToken)
                ?? throw new NotFoundException(nameof(Therapist), therapistId);
            entry.TherapistId = therapist.Id;
            entry.Therapist = therapist;
        }
        else
        {
            entry.TherapistId = null;
            entry.Therapist = null;
        }

        entry.DayOfWeek = request.DayOfWeek;
        entry.StartTime = request.StartTime;
        entry.EndTime = request.EndTime;
        entry.MaxSlotsPerDay = request.MaxSlotsPerDay;
        entry.IsActive = request.IsActive;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapSchedule(entry);
    }

    public async Task RemoveScheduleEntryAsync(Guid treatmentId, Guid entryId, CancellationToken cancellationToken)
    {
        var entry = await _treatments.GetScheduleEntryAsync(treatmentId, entryId, cancellationToken)
            ?? throw new NotFoundException(nameof(TreatmentSchedule), entryId);

        _treatments.RemoveSchedule(entry);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<TreatmentSchedule> BuildScheduleAsync(
        Treatment treatment,
        CreateScheduleEntryRequest request,
        CancellationToken cancellationToken)
    {
        Therapist? therapist = null;
        if (request.TherapistId is { } therapistId)
        {
            therapist = await _treatments.GetTherapistByIdAsync(therapistId, cancellationToken)
                ?? throw new NotFoundException(nameof(Therapist), therapistId);
        }

        return new TreatmentSchedule
        {
            TreatmentId = treatment.Id,
            Treatment = treatment,
            TherapistId = therapist?.Id,
            Therapist = therapist,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            MaxSlotsPerDay = request.MaxSlotsPerDay,
            IsActive = request.IsActive
        };
    }

    private static void EnsureUniqueSlot(
        Treatment treatment,
        Guid? therapistId,
        Weekday day,
        TimeOnly start,
        Guid? excludeEntryId)
    {
        var duplicate = treatment.Schedules.Any(s =>
            s.Id != excludeEntryId &&
            s.TherapistId == therapistId &&
            s.DayOfWeek == day &&
            s.StartTime == start);
        if (duplicate)
        {
            throw new ConflictException("A schedule entry already exists for this therapist, weekday, and start time.");
        }
    }

    private static TreatmentSummaryDto MapSummary(Treatment t) => new(
        t.Id,
        t.Name,
        t.NameSinhala,
        t.Description,
        t.DescriptionSinhala,
        t.Category,
        t.DurationMinutes,
        t.UnitPrice,
        t.IsActive,
        AvailableDays(t));

    private static TreatmentDetailDto MapDetail(Treatment t) => new(
        t.Id,
        t.Name,
        t.NameSinhala,
        t.Description,
        t.DescriptionSinhala,
        t.Category,
        t.DurationMinutes,
        t.UnitPrice,
        t.IsActive,
        AvailableDays(t),
        t.Schedules
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .Select(MapSchedule)
            .ToList());

    private static ScheduleEntryDto MapSchedule(TreatmentSchedule s) => new(
        s.Id,
        s.TreatmentId == Guid.Empty ? s.Treatment.Id : s.TreatmentId,
        s.TherapistId,
        s.Therapist?.FullName,
        s.DayOfWeek,
        s.StartTime,
        s.EndTime,
        s.MaxSlotsPerDay,
        s.IsActive);

    private static IReadOnlyList<DayOfWeek> AvailableDays(Treatment t) =>
        t.Schedules
            .Where(s => s.IsActive)
            .Select(s => ToSystemDay(s.DayOfWeek))
            .Distinct()
            .OrderBy(d => ((int)d + 6) % 7)
            .ToList();

    internal static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Monday,
        DayOfWeek.Tuesday => Weekday.Tuesday,
        DayOfWeek.Wednesday => Weekday.Wednesday,
        DayOfWeek.Thursday => Weekday.Thursday,
        DayOfWeek.Friday => Weekday.Friday,
        DayOfWeek.Saturday => Weekday.Saturday,
        DayOfWeek.Sunday => Weekday.Sunday,
        _ => throw new DomainException($"Unsupported day of week '{day}'.")
    };

    internal static DayOfWeek ToSystemDay(Weekday day) => day switch
    {
        Weekday.Monday => DayOfWeek.Monday,
        Weekday.Tuesday => DayOfWeek.Tuesday,
        Weekday.Wednesday => DayOfWeek.Wednesday,
        Weekday.Thursday => DayOfWeek.Thursday,
        Weekday.Friday => DayOfWeek.Friday,
        Weekday.Saturday => DayOfWeek.Saturday,
        Weekday.Sunday => DayOfWeek.Sunday,
        _ => throw new DomainException($"Unsupported weekday '{day}'.")
    };
}
