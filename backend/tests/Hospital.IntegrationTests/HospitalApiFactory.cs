using System.Net;
using System.Text;
using System.Text.Json;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Hospital.IntegrationTests;

[CollectionDefinition(HospitalApiCollection.Name)]
public sealed class HospitalApiCollection : ICollectionFixture<HospitalApiFactory>
{
    public const string Name = "HospitalApi";
}

public sealed class HospitalApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _root = new();

    public AgentHttpStub Agent { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<HospitalDbContext>));
            services.Remove(descriptor);

            services.AddDbContext<HospitalDbContext>(options =>
                options.UseInMemoryDatabase("hospital-tests", _root));

            // Replace the outbound handler for the typed agent client so tests never dial 127.0.0.1:8100.
            services.Configure<HttpClientFactoryOptions>("IAgentClient", options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                {
                    handlerBuilder.PrimaryHandler = Agent.CreateHandler();
                });
            });
        });
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
