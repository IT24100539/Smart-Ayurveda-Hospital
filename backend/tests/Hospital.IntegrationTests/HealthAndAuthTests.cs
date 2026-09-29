using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class HealthAndAuthTests
{
    private readonly HospitalApiFactory _factory;

    public HealthAndAuthTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Patients_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Treatments_WithoutToken_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/treatments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_OpensAPatientRecord()
    {
        var client = _factory.CreateClient();
        var email = $"new-{Guid.NewGuid():N}@example.local";
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Nimal Silva",
            email,
            phoneNumber = $"077{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1992, 3, 4),
            gender = Gender.Male
        }, json);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, "response body: {0}", body);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var patient = await db.Patients.SingleAsync(x => x.Email == email);
        patient.FirstName.Should().Be("Nimal");
        patient.LastName.Should().Be("Silva");
        patient.Uhid.Should().StartWith("SAH-");
        patient.DateOfBirth.Should().Be(new DateOnly(1992, 3, 4));
        patient.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public async Task Login_WithSeedAdmin_ReturnsToken()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@smartayurveda.local",
            password = "ChangeMe!Admin1"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        payload.Should().NotBeNull();
        payload!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PublicFeed_ShowsVisibleComments_AndHidesAnonymousNames()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/feedback");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Anonymous patient");
        json.Should().NotContain("therapist was dismissive");
        json.Should().NotContain("panchakarma package dates");

        var items = JsonSerializer.Deserialize<List<FeedItem>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? throw new InvalidOperationException("Public feed payload was empty.");

        var anonymous = items.Single(x => x.Comment.Contains("nadi pariksha slot", StringComparison.Ordinal));
        anonymous.IsAnonymous.Should().BeTrue();
        anonymous.PatientName.Should().Be("Anonymous patient");
        anonymous.PatientId.Should().BeNull();
        anonymous.Status.Should().Be("Visible");

        var named = items.Single(x => x.Comment.Contains("abhyanga session", StringComparison.Ordinal));
        named.IsAnonymous.Should().BeFalse();
        named.PatientName.Should().Be("Meera Nair");
        named.PatientId.Should().NotBeNull();
    }

    [Fact]
    public async Task StaffSearch_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/feedback/staff");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFeedback_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/feedback", new
        {
            rating = 5,
            comment = "The abhyanga was calming.",
            isAnonymous = true
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginPayload(string Token);
    private sealed record FeedItem(
        Guid? PatientId,
        string PatientName,
        string Comment,
        bool IsAnonymous,
        string Status);
}
