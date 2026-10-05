using System.Net;
using System.Text;
using System.Text.Json;
using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;

namespace Hospital.IntegrationTests;

[CollectionDefinition(HospitalApiCollection.Name)]
public sealed class HospitalApiCollection : ICollectionFixture<HospitalApiFactory>
{
    public const string Name = "HospitalApi";
}

public class HospitalApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _root = new();
    private readonly IReadOnlyDictionary<string, string?>? _configurationOverrides;

    public const string InternalServiceKey = "dev-internal-service-key";

    public HospitalApiFactory()
        : this(null)
    {
    }

    protected HospitalApiFactory(IReadOnlyDictionary<string, string?>? configurationOverrides)
    {
        _configurationOverrides = configurationOverrides;
    }

    public AgentHttpStub Agent { get; } = new();

    private static readonly object HostBuildGate = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        lock (HostBuildGate)
        {
            ApplyProcessConfiguration();
            var host = base.CreateHost(builder);
            RestoreSharedLimits();
            return host;
        }
    }

    protected virtual void ApplyProcessConfiguration()
    {
        // Environment variables outrank appsettings and user-secrets. The test host
        // must not depend on secrets stored in either place.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-32chars!!");
        Environment.SetEnvironmentVariable("AgentService__SharedSecret", "integration-test-agent-secret");
        Environment.SetEnvironmentVariable("INTERNAL_SERVICE_KEY", InternalServiceKey);
        Environment.SetEnvironmentVariable("Auth__RateLimit__PermitLimit", "10000");
        Environment.SetEnvironmentVariable("Auth__RateLimit__WindowSeconds", "60");
        Environment.SetEnvironmentVariable("Auth__Lockout__MaxFailedAttempts", "5");
        Environment.SetEnvironmentVariable("Auth__Lockout__LockoutMinutes", "15");
    }

    private static void RestoreSharedLimits()
    {
        Environment.SetEnvironmentVariable("Auth__RateLimit__PermitLimit", "10000");
        Environment.SetEnvironmentVariable("Auth__RateLimit__WindowSeconds", "60");
        Environment.SetEnvironmentVariable("Auth__Lockout__MaxFailedAttempts", "5");
        Environment.SetEnvironmentVariable("Auth__Lockout__LockoutMinutes", "15");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Last source wins, including over Development user-secrets, so these tests
        // send a known key and a wrong key is rejected.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["INTERNAL_SERVICE_KEY"] = InternalServiceKey,
                ["InternalServiceKey"] = InternalServiceKey,
                ["InternalService:ApiKey"] = InternalServiceKey,
                ["InternalService:Key"] = InternalServiceKey,
                ["Jwt:SigningKey"] = "integration-test-signing-key-32chars!!",
                ["AgentService:SharedSecret"] = "integration-test-agent-secret",
                ["Auth:RateLimit:PermitLimit"] = "10000",
                ["Auth:RateLimit:WindowSeconds"] = "60",
                ["DoctorPhotos:StoragePath"] = Path.Combine(Path.GetTempPath(), "smart-ayurveda-hospital-tests", "doctor-photos"),
                ["MedicalDocuments:StoragePath"] = Path.Combine(Path.GetTempPath(), "smart-ayurveda-hospital-tests", "medical-documents"),
            };
            if (_configurationOverrides is not null)
            {
                foreach (var pair in _configurationOverrides)
                {
                    settings[pair.Key] = pair.Value;
                }
            }

            config.AddInMemoryCollection(settings);
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<HospitalDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<HospitalDbContext>((sp, options) =>
                options.UseInMemoryDatabase("hospital-tests", _root)
                    .AddInterceptors(sp.GetRequiredService<ClinicalAuditInterceptor>()));

            // Replace the outbound handler for the typed agent client so tests never dial 127.0.0.1:8001.
            services.Configure<HttpClientFactoryOptions>("IAgentClient", options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                {
                    handlerBuilder.PrimaryHandler = Agent.CreateHandler();
                });
            });
        });
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, UserRole role)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        if (!db.Users.Any(u => u.Id == userId))
        {
            db.Users.Add(new User
            {
                Id = userId,
                FullName = $"Integration {role}",
                Email = $"{role.ToString().ToLowerInvariant()}-{userId:N}@integration.test",
                PhoneNumber = UniquePhone(userId),
                Role = role,
                IsActive = true,
                TokenVersion = 1,
                PasswordHash = "test-hash"
            });
            db.SaveChanges();
        }

        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var user = db.Users.First(u => u.Id == userId);
        var (token, _) = tokens.Create(user);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string UniquePhone(Guid userId)
    {
        var hex = userId.ToString("N");
        var digits = new char[10];
        for (var i = 0; i < digits.Length; i++)
        {
            digits[i] = (char)('0' + (hex[i] % 10));
        }

        return new string(digits);
    }
}

/// <summary>
/// Stands in for the internal agent-service HTTP endpoint. Each typed client gets its own handler,
/// and every call is counted on this shared stub.
/// </summary>
public sealed class AgentHttpStub
{
    public int Calls { get; private set; }
    public bool Fail { get; set; }
    public string? LastPath { get; private set; }
    public string? LastSecret { get; private set; }
    public string Reply { get; set; } =
        "Namaste. We have noted your feedback about the completed nadi pariksha and will follow up with the kayachikitsa team.";

    public HttpMessageHandler CreateHandler() => new Handler(this);

    private sealed class Handler : HttpMessageHandler
    {
        private readonly AgentHttpStub _stub;

        public Handler(AgentHttpStub stub) => _stub = stub;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _stub.Calls++;
            _stub.LastPath = request.RequestUri?.AbsolutePath;
            if (_stub.Fail)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }
            _stub.LastSecret = request.Headers.TryGetValues("X-Internal-Secret", out var secrets)
                ? secrets.FirstOrDefault()
                : null;

            var path = request.RequestUri?.AbsolutePath ?? "";
            var json = path.Contains("feedback-support", StringComparison.Ordinal)
                ? JsonSerializer.Serialize(new
                {
                    sentiment = "Positive",
                    category = "TreatmentQuality",
                    priority = "Normal",
                    similar_feedback_count = 0,
                    suggested_reply = _stub.Reply,
                    draft_skipped = false,
                    workflow_id = "wf-test",
                    status = "awaiting_review",
                    immediate_dashboard_alert = false
                })
                : JsonSerializer.Serialize(new
                {
                    agent = "feedback",
                    reply = _stub.Reply,
                    metadata = new Dictionary<string, string>()
                });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
