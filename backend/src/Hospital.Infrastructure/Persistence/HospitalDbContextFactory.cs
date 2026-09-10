using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Hospital.Infrastructure.Persistence;

public sealed class HospitalDbContextFactory : IDesignTimeDbContextFactory<HospitalDbContext>
{
    public HospitalDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var basePath = FindApiPath();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var options = new DbContextOptionsBuilder<HospitalDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new HospitalDbContext(options);
    }

    private static string FindApiPath()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var api = Path.Combine(current.FullName, "src", "Hospital.Api");
            if (File.Exists(Path.Combine(api, "appsettings.json")))
            {
                return api;
            }

            api = Path.Combine(current.FullName, "Hospital.Api");
            if (File.Exists(Path.Combine(api, "appsettings.json")))
            {
                return api;
            }

            if (File.Exists(Path.Combine(current.FullName, "appsettings.json"))
                && current.Name == "Hospital.Api")
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Hospital.Api appsettings.json for design-time DbContext.");
    }
}
