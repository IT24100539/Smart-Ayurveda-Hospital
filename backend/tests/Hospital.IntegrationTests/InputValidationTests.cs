using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Hospital.Api.Middleware;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hospital.IntegrationTests;

public sealed class InputValidationTests : IClassFixture<HospitalApiFactory>
{
    private readonly HospitalApiFactory _factory;

    public InputValidationTests(HospitalApiFactory factory) => _factory = factory;

    [Fact]
    public async Task MalformedJson_ReturnsProblemDetailsWithoutInternals()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/login", new StringContent("{", Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        AssertProblemShape(body);
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stack trace", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OversizedBody_Returns413ProblemDetails()
    {
        var client = _factory.CreateClient();
        var payload = "{\"email\":\"" + new string('a', 1_048_576) + "\"}";
        var response = await client.PostAsync("/api/auth/login", new StringContent(payload, Encoding.UTF8, "application/json"));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        AssertProblemShape(body);
        Assert.Contains("too large", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidRouteId_ReturnsNotFoundProblemDetails()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.Doctor);
        var response = await client.GetAsync("/api/patients/not-a-guid");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        AssertProblemShape(body);
    }

    [Fact]
    public async Task InvalidBodyId_ReturnsValidationProblemDetails()
    {
        var client = _factory.CreateAuthenticatedClient(Guid.NewGuid(), UserRole.FrontDeskStaff);
        var response = await client.PostAsJsonAsync("/api/appointments", new
        {
            patientId = "not-a-guid",
            treatmentId = Guid.NewGuid(),
            requestedDate = new DateOnly(2026, 10, 6),
            requestedTimeSlot = "Morning"
        });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("problem+json", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
        AssertProblemShape(body);
    }

    [Fact]
    public async Task ProductionUnhandledException_OmitsSqlStackAndPaths()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException(
                "Npgsql.PostgresException 42P01: SELECT * FROM users at D:\\smart-ayurveda-hospital\\backend\\src\\Hospital.Api\\Program.cs:line 12"),
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            new TestEnvironment { EnvironmentName = Environments.Production });
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        AssertProblemShape(body);
        Assert.DoesNotContain("SELECT", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Program.cs", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smart-ayurveda-hospital", body, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertProblemShape(string body)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        Assert.True(root.TryGetProperty("type", out var type) && type.GetString()!.StartsWith("https://", StringComparison.Ordinal));
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.True(root.GetProperty("status").GetInt32() >= 400);
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("detail").GetString()));
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Hospital.Api";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
