using Hospital.Application.Communication;
using Hospital.Application.Communication.Dtos;
using Hospital.Application.Communication.Validators;
using Hospital.Domain.Entities;
using Hospital.Domain.Exceptions;

namespace Hospital.UnitTests;

public sealed class DeviceTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 2, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_StoresTheTokenOnTheSignedInChart()
    {
        var chart = Patient("Leela");
        var tokens = new InMemoryDeviceTokenRepository();
        var sut = Service(tokens, chart);

        var saved = await sut.RegisterAsync(new RegisterDeviceTokenRequest("  phone-token-1  ", "Android"), CancellationToken.None);

        var row = Assert.Single(tokens.Items);
        Assert.Equal(chart.Id, row.PatientId);
        Assert.NotEqual(chart.Id, Guid.Empty);
        Assert.Equal("phone-token-1", row.Token);
        Assert.Equal(PatientDeviceToken.Android, row.Platform);
        Assert.Equal(saved.Id, row.Id);
        Assert.Equal(PatientDeviceToken.Android, saved.Platform);
    }

    [Fact]
    public async Task Register_RejectsStaff()
    {
        var tokens = new InMemoryDeviceTokenRepository();
        var sut = Service(tokens, patient: null);

        var error = await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.RegisterAsync(new RegisterDeviceTokenRequest("phone-token-1", "android"), CancellationToken.None));

        Assert.Contains("patient", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(tokens.Items);
    }

    [Fact]
    public async Task Register_MovesATokenToThePatientWhoNowHoldsTheDevice()
    {
        var first = Patient("Leela");
        var second = Patient("Arun");
        var tokens = new InMemoryDeviceTokenRepository();
        var firstService = Service(tokens, first);
        var secondService = Service(tokens, second);

        await firstService.RegisterAsync(new RegisterDeviceTokenRequest("shared-handset", "android"), CancellationToken.None);
        await secondService.RegisterAsync(new RegisterDeviceTokenRequest("shared-handset", "ios"), CancellationToken.None);

        var row = Assert.Single(tokens.Items);
        Assert.Equal(second.Id, row.PatientId);
        Assert.Equal(PatientDeviceToken.Ios, row.Platform);
        Assert.Empty(await tokens.ListTokensForPatientAsync(first.Id, CancellationToken.None));
        Assert.Equal("shared-handset", Assert.Single(await tokens.ListTokensForPatientAsync(second.Id, CancellationToken.None)));
    }

    [Fact]
    public void Validator_RejectsABlankTokenAndAnUnknownPlatform()
    {
        var validator = new RegisterDeviceTokenRequestValidator();

        var blank = validator.Validate(new RegisterDeviceTokenRequest("  ", "android"));
        Assert.False(blank.IsValid);

        var platform = validator.Validate(new RegisterDeviceTokenRequest("phone-token-1", "windows"));
        Assert.False(platform.IsValid);
    }

    private static DeviceTokenService Service(InMemoryDeviceTokenRepository tokens, Patient? patient) =>
        new(tokens, new ScriptActor(patient), new NoopUnitOfWork(), new FixedClock());

    private static Patient Patient(string firstName) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = firstName,
        LastName = "Menon",
        Uhid = "SAH-2026-00031"
    };

    private sealed class FixedClock : Hospital.Application.Abstractions.IClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
