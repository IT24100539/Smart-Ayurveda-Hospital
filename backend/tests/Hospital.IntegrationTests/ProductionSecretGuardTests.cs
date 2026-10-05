using FluentAssertions;
using Hospital.Api.Security;
using Microsoft.Extensions.Configuration;

namespace Hospital.IntegrationTests;

public sealed class ProductionSecretGuardTests
{
    [Fact]
    public void Validate_MissingSecrets_NamesTheKeysAndNotAnyValue()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var error = Assert.Throws<InvalidOperationException>(() =>
            ProductionSecretGuard.Validate(configuration, tlsTerminatedByHost: true));

        error.Message.Should().Contain("ConnectionStrings:DefaultConnection");
        error.Message.Should().Contain("Jwt:SigningKey");
        error.Message.Should().Contain("AgentService:SharedSecret");
        error.Message.Should().Contain("InternalService:ApiKey");
        error.Message.Should().Contain("CORS origins");
    }

    [Fact]
    public void Validate_DevelopmentPlaceholdersAndLocalOrigins_AreRejected()
    {
        var configuration = Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Username=postgres;Password=change-me",
            ["Jwt:SigningKey"] = "replace-me-with-at-least-32-random-characters",
            ["AgentService:SharedSecret"] = "replace-me-agent",
            ["InternalService:ApiKey"] = "replace-me-internal",
            ["AllowedOrigins"] = "http://localhost:5173,https://127.0.0.1:5174"
        });

        var error = Assert.Throws<InvalidOperationException>(() =>
            ProductionSecretGuard.Validate(configuration, tlsTerminatedByHost: true));

        error.Message.Should().Contain("development placeholder");
        error.Message.Should().Contain("https staff web");
    }

    [Fact]
    public void Validate_RequiresACertificateWhenTheProcessTerminatesTls()
    {
        var configuration = Build(AcceptableSecrets());

        var error = Assert.Throws<InvalidOperationException>(() =>
            ProductionSecretGuard.Validate(configuration, tlsTerminatedByHost: false));

        error.Message.Should().Contain("Kestrel:Certificates:Default:Path");
        error.Message.Should().Contain("Kestrel:Certificates:Default:Password");
    }

    [Fact]
    public void Validate_AcceptsDistinctSecretsWhenTheHostTerminatesTls()
    {
        var configuration = Build(AcceptableSecrets());

        var act = () => ProductionSecretGuard.Validate(configuration, tlsTerminatedByHost: true);

        act.Should().NotThrow();
    }

    private static Dictionary<string, string?> AcceptableSecrets() => new()
    {
        ["ConnectionStrings:DefaultConnection"] = "Host=db.internal;Username=hospital;Password=n0t-a-placeholder-value",
        ["Jwt:SigningKey"] = "abc123XYZ789abc123XYZ789abc123XYZ789",
        ["AgentService:SharedSecret"] = "agent-secret-value-not-a-placeholder",
        ["InternalService:ApiKey"] = "internal-key-value-not-a-placeholder",
        ["AllowedOrigins"] = "https://staff.hospital.test,https://patients.hospital.test"
    };

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
