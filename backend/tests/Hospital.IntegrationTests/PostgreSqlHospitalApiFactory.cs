using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Hospital.IntegrationTests;

public sealed class PostgreSqlHospitalApiFactory : WebApplicationFactory<Program>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__IntegrationTests";
    private const string RequiredDatabaseName = "ayurveda_hospital_test";
    private const string DevelopmentDatabaseName = "ayurveda_hospital";
    private const long DatabaseResetAdvisoryLockKey = 4815162342;
    private readonly string _connectionString = GetValidatedConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HospitalDbContext>>();
            services.AddDbContext<HospitalDbContext>(options =>
                options.UseNpgsql(_connectionString));
        });
    }

    private static readonly SemaphoreSlim DatabaseResetLock = new(1, 1);

    public async Task ResetDatabaseAsync()
    {
        await DatabaseResetLock.WaitAsync();

        try
        {
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();

            await db.Database.OpenConnectionAsync();

            await using var transaction = await db.Database.BeginTransactionAsync();

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({DatabaseResetAdvisoryLockKey});");

            await db.Database.ExecuteSqlRawAsync("""
                DROP SCHEMA IF EXISTS public CASCADE;
                CREATE SCHEMA public;
                """);

            await db.Database.MigrateAsync();

            await transaction.CommitAsync();
        }
        finally
        {
            DatabaseResetLock.Release();
        }
    }

    private static string GetValidatedConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"PostgreSQL integration tests require the {ConnectionStringEnvironmentVariable} environment variable. " +
                "Set it to a connection string whose Database is ayurveda_hospital_test. " +
                "The integration tests will not use the development database.");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOperationException(
                $"The {ConnectionStringEnvironmentVariable} environment variable is not a valid PostgreSQL connection string.",
                exception);
        }

        if (string.Equals(builder.Database, DevelopmentDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to run integration tests against development database '{DevelopmentDatabaseName}'.");
        }

        if (!string.Equals(builder.Database, RequiredDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Integration tests may run only against the dedicated database '{RequiredDatabaseName}', " +
                $"but the configured database is '{builder.Database}'.");
        }

        return builder.ConnectionString;
    }

    public HttpClient CreateAuthenticatedClient(Guid userId, UserRole role)
    {
        using var scope = Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var (token, _) = tokens.Create(new User
        {
            Id = userId,
            FullName = $"Integration {role}",
            Email = $"{role.ToString().ToLowerInvariant()}-{userId:N}@integration.test",
            PhoneNumber = "0000000000",
            Role = role,
            IsActive = true
        });

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
