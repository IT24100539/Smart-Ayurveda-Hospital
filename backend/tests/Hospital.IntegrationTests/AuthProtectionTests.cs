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

public sealed class LockoutApiFactory : HospitalApiFactory
{
    public LockoutApiFactory()
        : base(new Dictionary<string, string?>
        {
            ["Auth:Lockout:MaxFailedAttempts"] = "3",
            ["Auth:Lockout:LockoutMinutes"] = "15"
        })
    {
    }

    protected override void ApplyProcessConfiguration()
    {
        base.ApplyProcessConfiguration();
        Environment.SetEnvironmentVariable("Auth__Lockout__MaxFailedAttempts", "3");
        Environment.SetEnvironmentVariable("Auth__Lockout__LockoutMinutes", "15");
    }
}

public sealed class RateLimitApiFactory : HospitalApiFactory
{
    public RateLimitApiFactory()
        : base(new Dictionary<string, string?>
        {
            ["Auth:RateLimit:PermitLimit"] = "2",
            ["Auth:RateLimit:WindowSeconds"] = "60"
        })
    {
    }

    protected override void ApplyProcessConfiguration()
    {
        base.ApplyProcessConfiguration();
        Environment.SetEnvironmentVariable("Auth__RateLimit__PermitLimit", "2");
        Environment.SetEnvironmentVariable("Auth__RateLimit__WindowSeconds", "60");
    }
}

public sealed class AuthLockoutTests : IClassFixture<LockoutApiFactory>
{
    private readonly LockoutApiFactory _factory;

    public AuthLockoutTests(LockoutApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_LocksAfterRepeatedFailures_AndUnlocksWhenTheWindowHasPassed()
    {
        var client = _factory.CreateClient();
        var email = $"lock.{Guid.NewGuid():N}@example.local";
        const string password = "ChangeMe!Patient1";

        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Lockout Patient",
            email,
            phoneNumber = $"077{Random.Shared.Next(1000000, 9999999)}",
            password,
            dateOfBirth = new DateOnly(1991, 5, 6),
            gender = Gender.Female
        }, Json);

        var registeredBody = await registered.Content.ReadAsStringAsync();
        registered.StatusCode.Should().Be(HttpStatusCode.OK, registeredBody);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email,
                password = "WrongPass!111"
            });
            var failedBody = await failed.Content.ReadAsStringAsync();
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            failedBody.Should().Contain("Invalid email or password");
            failedBody.Should().NotContain(email);
        }

        var locked = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var lockedBody = await locked.Content.ReadAsStringAsync();
        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        lockedBody.Should().Contain("temporarily locked");
        lockedBody.Should().NotContain(email);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var user = await db.Users.SingleAsync(x => x.Email == email);
            user.LockoutEnd.Should().NotBeNull();
            user.LockoutEnd.Should().BeAfter(DateTimeOffset.UtcNow);
            user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var unlocked = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        unlocked.StatusCode.Should().Be(HttpStatusCode.OK, await unlocked.Content.ReadAsStringAsync());

        await using var check = _factory.Services.CreateAsyncScope();
        var store = check.ServiceProvider.GetRequiredService<HospitalDbContext>();
        var cleared = await store.Users.SingleAsync(x => x.Email == email);
        cleared.FailedLoginCount.Should().Be(0);
        cleared.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task Register_DuplicateEmail_DoesNotRevealThatTheEmailExists()
    {
        var client = _factory.CreateClient();
        var email = $"taken.{Guid.NewGuid():N}@example.local";
        var first = await RegisterAsync(client, email, $"077{Random.Shared.Next(1000000, 9999999)}");
        first.EnsureSuccessStatusCode();

        var second = await RegisterAsync(client, email, $"071{Random.Shared.Next(1000000, 9999999)}");
        var body = await second.Content.ReadAsStringAsync();
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body.Should().Contain("Unable to create an account with the details provided.");
        body.Should().NotContain(email);
        body.ToLowerInvariant().Should().NotContain("already exists");
    }

    [Fact]
    public async Task Login_UnknownEmail_UsesTheSameMessageAsAWrongPassword()
    {
        var client = _factory.CreateClient();
        var email = $"missing.{Guid.NewGuid():N}@example.local";
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPass!111"
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.Should().Contain("Invalid email or password");
        body.Should().NotContain(email);
        body.Should().NotContain("locked");
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsEachUnmetRule()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Weak Password",
            email = $"weak.{Guid.NewGuid():N}@example.local",
            phoneNumber = $"076{Random.Shared.Next(1000000, 9999999)}",
            password = "abc",
            dateOfBirth = new DateOnly(1994, 2, 2),
            gender = Gender.Male
        }, Json);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("at least 8 characters");
        body.Should().Contain("uppercase letter");
        body.Should().Contain("digit");
        body.Should().Contain("special character");
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string phone) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Generic Patient",
            email,
            phoneNumber = phone,
            password = "ChangeMe!Patient1",
            dateOfBirth = new DateOnly(1992, 3, 4),
            gender = Gender.Male
        }, Json);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class AuthRateLimitTests : IClassFixture<RateLimitApiFactory>
{
    private readonly RateLimitApiFactory _factory;

    public AuthRateLimitTests(RateLimitApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/auth/login", "login")]
    [InlineData("/api/auth/register", "register")]
    [InlineData("/api/auth/forgot-password", "reset")]
    [InlineData("/api/auth/reset-password", "reset-complete")]
    public async Task AuthEndpoints_Return429AfterTheConfiguredPermit(string path, string label)
    {
        var client = _factory.CreateClient();
        var body = label switch
        {
            "login" => (object)new { email = $"{label}@example.local", password = "WrongPass!111" },
            "register" => new
            {
                fullName = "Rate Limit",
                email = $"{label}.{Guid.NewGuid():N}@example.local",
                phoneNumber = "0770000000",
                password = "x"
            },
            "reset" => new { email = $"{label}.{Guid.NewGuid():N}@example.local" },
            _ => new
            {
                email = $"{label}.{Guid.NewGuid():N}@example.local",
                token = "not-a-real-token",
                newPassword = "x",
                confirmPassword = "x"
            }
        };

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var allowed = await PostAsync(client, path, body);
            allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var blocked = await PostAsync(client, path, body);
        var text = await blocked.Content.ReadAsStringAsync();
        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        text.Should().Contain("Too many attempts");
        text.Should().NotContain("@");
    }

    private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, object body) =>
        client.PostAsync(path, JsonContent.Create(body, body.GetType()));
}
