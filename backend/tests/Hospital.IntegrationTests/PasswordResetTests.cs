using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.IntegrationTests;

[Collection(HospitalApiCollection.Name)]
public sealed class PasswordResetTests
{
    private readonly HospitalApiFactory _factory;

    public PasswordResetTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RequestReset_UnknownAndKnownEmail_ReturnTheSameBody()
    {
        var client = _factory.CreateClient();
        var knownEmail = await RegisterPatientAsync(client);

        var known = await client.PostAsJsonAsync("/api/auth/request-reset", new { email = knownEmail });
        var unknown = await client.PostAsJsonAsync("/api/auth/request-reset", new { email = $"missing.{Guid.NewGuid():N}@example.local" });

        known.StatusCode.Should().Be(HttpStatusCode.OK);
        unknown.StatusCode.Should().Be(HttpStatusCode.OK);
        (await known.Content.ReadAsStringAsync()).Should().Be(await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CompleteReset_Success_AllowsNewPasswordAndRevokesExistingSession()
    {
        var client = _factory.CreateClient();
        var email = await RegisterPatientAsync(client);
        var login = await LoginAsync(client, email, "ChangeMe!Patient1");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.Token);

        var rawToken = SeedResetToken(email, TimeSpan.FromMinutes(20));
        var complete = await client.PostAsJsonAsync("/api/auth/complete-reset", new
        {
            email,
            token = rawToken,
            newPassword = "NewPass!2345",
            confirmPassword = "NewPass!2345"
        });
        complete.StatusCode.Should().Be(HttpStatusCode.OK);

        var stale = await client.GetAsync("/api/patients/me/registration-summary");
        stale.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var freshLogin = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "NewPass!2345"
        });
        freshLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        var oldPassword = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "ChangeMe!Patient1"
        });
        oldPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CompleteReset_ExpiredToken_IsRejected()
    {
        var client = _factory.CreateClient();
        var email = await RegisterPatientAsync(client);
        var rawToken = SeedResetToken(email, TimeSpan.FromMinutes(-1));

        var response = await client.PostAsJsonAsync("/api/auth/complete-reset", new
        {
            email,
            token = rawToken,
            newPassword = "NewPass!2345",
            confirmPassword = "NewPass!2345"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteReset_ReusedToken_IsRejected()
    {
        var client = _factory.CreateClient();
        var email = await RegisterPatientAsync(client);
        var rawToken = SeedResetToken(email, TimeSpan.FromMinutes(20));
        var body = new
        {
            email,
            token = rawToken,
            newPassword = "NewPass!2345",
            confirmPassword = "NewPass!2345"
        };

        var first = await client.PostAsJsonAsync("/api/auth/complete-reset", body);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await client.PostAsJsonAsync("/api/auth/complete-reset", body);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteReset_WeakNewPassword_IsRejected()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/complete-reset", new
        {
            email = "reset@example.local",
            token = "any-token",
            newPassword = "weak",
            confirmPassword = "weak"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Password");
    }

    private async Task<string> RegisterPatientAsync(HttpClient client)
    {
        var email = $"reset.{Guid.NewGuid():N}@example.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Reset Patient",
            email,
            phoneNumber = $"077{Random.Shared.Next(1000000, 9999999)}",
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1991, 4, 4),
            gender = Gender.Female
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return email;
    }

    private async Task<LoginPayload> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LoginPayload>())!;
    }

    private string SeedResetToken(string email, TimeSpan lifetime)
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var user = db.Users.Single(x => x.Email == email);
        user.PasswordResetTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        user.PasswordResetTokenExpiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        db.SaveChanges();
        return rawToken;
    }

    private sealed record LoginPayload(string Token);
}
