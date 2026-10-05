using Hospital.Application.Common;
using Hospital.Application.Patients.Dtos;
using Hospital.Application.Patients.Validators;
using Hospital.Domain.Enums;

namespace Hospital.UnitTests;

public class RequestValidatorTests
{
    [Fact]
    public void CreatePatient_RejectsFutureBirthDateAndUnknownDosha()
    {
        var validator = new CreatePatientRequestValidator();
        var result = validator.Validate(new CreatePatientRequest(
            "Meera",
            "Iyer",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Gender.Female,
            "0771234567",
            null,
            null,
            null,
            null,
            (DoshaType)128,
            DoshaType.Vata));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePatientRequest.DateOfBirth));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreatePatientRequest.Prakriti));
    }

    [Fact]
    public void CalendarDate_RejectsDefaultAndFarFuture()
    {
        Assert.False(DateSanity.IsCalendarDate(default));
        Assert.False(DateSanity.IsCalendarDate(new DateOnly(1899, 12, 31)));
        Assert.True(DateSanity.IsCalendarDate(new DateOnly(2026, 10, 6)));
        Assert.False(DateSanity.IsBirthDate(new DateOnly(2100, 1, 1)));
    }
}
