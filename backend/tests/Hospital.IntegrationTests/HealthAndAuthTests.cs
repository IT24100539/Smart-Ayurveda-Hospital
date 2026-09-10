using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Hospital.IntegrationTests;

public sealed class HealthAndAuthTests : IClassFixture<HospitalApiFactory>
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

    private sealed record LoginPayload(string Token);
}
