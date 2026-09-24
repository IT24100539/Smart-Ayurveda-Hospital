using Hospital.Application.Abstractions;
using Hospital.Application.Treatments;
using Hospital.Application.Treatments.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class TreatmentServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsDistinctAvailableDays_FromActiveSchedule()
    {
        var treatment = NewTreatment("Shirodhara");
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Tuesday, isActive: true));
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Thursday, isActive: true));
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Saturday, isActive: false));

        var sut = CreateSut(treatment);
        var result = await sut.SearchAsync(new TreatmentSearchQuery(), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, item.AvailableDays);
    }

    [Fact]
    public async Task IsAvailableOnAsync_WhenWeekdayHasActiveSlot_ReturnsRemainingSlots()
    {
        var treatment = NewTreatment("Nasya");
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Tuesday, maxSlots: 8));
        var counts = new FakeAppointmentCountProvider { Booked = 3 };
        var sut = CreateSut(treatment, counts);

        var tuesday = Next(DayOfWeek.Tuesday);
        var availability = await sut.IsAvailableOnAsync(treatment.Id, tuesday, CancellationToken.None);

        Assert.True(availability.IsAvailable);
        Assert.Equal(8, availability.MaxSlotsPerDay);
        Assert.Equal(3, availability.BookedCount);
        Assert.Equal(5, availability.RemainingSlots);
    }

    [Fact]
    public async Task IsAvailableOnAsync_WhenNoMatchingWeekday_IsUnavailable()
    {
        var treatment = NewTreatment("Nasya");
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Tuesday, maxSlots: 8));
        var sut = CreateSut(treatment);

        var monday = Next(DayOfWeek.Monday);
        var availability = await sut.IsAvailableOnAsync(treatment.Id, monday, CancellationToken.None);

        Assert.False(availability.IsAvailable);
        Assert.Equal(0, availability.RemainingSlots);
    }

    [Fact]
    public async Task ValidateBookingDateAsync_WhenNoSchedule_ThrowsInvalidSchedule()
    {
        var treatment = NewTreatment("Panchakarma");
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Friday));
        var sut = CreateSut(treatment);

        var sunday = Next(DayOfWeek.Sunday);
        await Assert.ThrowsAsync<InvalidScheduleException>(
            () => sut.ValidateBookingDateAsync(treatment.Id, sunday, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateBookingDateAsync_WhenActiveScheduleMatches_Succeeds()
    {
        var treatment = NewTreatment("Panchakarma");
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Monday));
        var sut = CreateSut(treatment);

        await sut.ValidateBookingDateAsync(treatment.Id, Next(DayOfWeek.Monday), CancellationToken.None);
    }

    [Fact]
    public async Task ValidateBookingDateAsync_WhenTreatmentInactive_ThrowsInvalidSchedule()
    {
        var treatment = NewTreatment("Panchakarma");
        treatment.IsActive = false;
        treatment.Schedules.Add(NewSlot(treatment, Weekday.Monday));
        var sut = CreateSut(treatment);

        await Assert.ThrowsAsync<InvalidScheduleException>(
            () => sut.ValidateBookingDateAsync(treatment.Id, Next(DayOfWeek.Monday), CancellationToken.None));
    }

    [Fact]
    public async Task DeactivateAsync_SetsIsActiveFalse_WithoutDeletingTheRow()
    {
        var treatment = NewTreatment("Herbal Steam");
        var repo = new FakeTreatmentRepository(treatment);
        var sut = new TreatmentService(repo, new FakeAppointmentCountProvider(), new FakeUnitOfWork());

        var result = await sut.DeactivateAsync(treatment.Id, CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.False(treatment.IsActive);
        Assert.Single(repo.Items);
        Assert.Same(treatment, repo.Items[0]);
        Assert.NotNull(await repo.GetByIdAsync(treatment.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AddScheduleEntryAsync_RejectsDuplicateSlot()
    {
        var treatment = NewTreatment("General Consultation");
        var existing = NewSlot(treatment, Weekday.Wednesday);
        existing.StartTime = new TimeOnly(14, 0);
        treatment.Schedules.Add(existing);
        var sut = CreateSut(treatment);

        var request = new CreateScheduleEntryRequest(
            Weekday.Wednesday,
            new TimeOnly(14, 0),
            new TimeOnly(16, 0),
            10,
            null);

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.AddScheduleEntryAsync(treatment.Id, request, CancellationToken.None));
    }

    private static ITreatmentService CreateSut(Treatment treatment, IAppointmentCountProvider? counts = null) =>
        new TreatmentService(
            new FakeTreatmentRepository(treatment),
            counts ?? new FakeAppointmentCountProvider(),
            new FakeUnitOfWork());

    private static Treatment NewTreatment(string name) => new()
    {
        Name = name,
        NameSinhala = name,
        Description = "Seeded for tests",
        DescriptionSinhala = "Seeded for tests",
        Category = TreatmentCategory.General,
        DurationMinutes = 30,
        UnitPrice = 500,
        IsActive = true
    };

    private static TreatmentSchedule NewSlot(Treatment treatment, Weekday day, int maxSlots = 6, bool isActive = true) =>
        new()
        {
            TreatmentId = treatment.Id,
            Treatment = treatment,
            DayOfWeek = day,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            MaxSlotsPerDay = maxSlots,
            IsActive = isActive
        };

    private static DateOnly Next(DayOfWeek day)
    {
        var date = new DateOnly(2026, 9, 14);
        while (date.DayOfWeek != day)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class FakeAppointmentCountProvider : IAppointmentCountProvider
    {
        public int Booked { get; init; }

        public Task<int> CountBookedSlotsAsync(Guid treatmentId, DateOnly date, CancellationToken cancellationToken) =>
            Task.FromResult(Booked);
    }

    private sealed class FakeTreatmentRepository : ITreatmentRepository
    {
        private readonly List<Treatment> _items;
        private readonly List<Therapist> _therapists = new();

        public FakeTreatmentRepository(params Treatment[] treatments) => _items = treatments.ToList();

        public IReadOnlyList<Treatment> Items => _items;

        public Task<Treatment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<Treatment?> GetByIdWithScheduleAsync(Guid id, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);

        public Task<(IReadOnlyList<Treatment> Items, int Total)> SearchAsync(
            string? name,
            TreatmentCategory? category,
            bool? activeOnly,
            int page,
            int pageSize,
            string? sort,
            CancellationToken cancellationToken)
        {
            IEnumerable<Treatment> q = _items;
            if (!string.IsNullOrWhiteSpace(name))
            {
                q = q.Where(x => x.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }

            if (category is not null)
            {
                q = q.Where(x => x.Category == category);
            }

            if (activeOnly == true)
            {
                q = q.Where(x => x.IsActive);
            }

            var list = q.ToList();
            return Task.FromResult(((IReadOnlyList<Treatment>)list, list.Count));
        }

        public Task AddAsync(Treatment treatment, CancellationToken cancellationToken)
        {
            _items.Add(treatment);
            return Task.CompletedTask;
        }

        public Task<TreatmentSchedule?> GetScheduleEntryAsync(Guid treatmentId, Guid entryId, CancellationToken cancellationToken)
        {
            var treatment = _items.FirstOrDefault(x => x.Id == treatmentId);
            return Task.FromResult(treatment?.Schedules.FirstOrDefault(s => s.Id == entryId));
        }

        public Task AddScheduleAsync(TreatmentSchedule entry, CancellationToken cancellationToken)
        {
            entry.Treatment.Schedules.Add(entry);
            return Task.CompletedTask;
        }

        public void RemoveSchedule(TreatmentSchedule entry) =>
            entry.Treatment.Schedules.Remove(entry);

        public Task<Therapist?> GetTherapistByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_therapists.FirstOrDefault(x => x.Id == id));
    }
}
