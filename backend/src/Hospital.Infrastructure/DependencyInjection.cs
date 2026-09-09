using System.Text;
using Hospital.Application.Abstractions;
using Hospital.Application.Agents;
using Hospital.Infrastructure.Agents;
using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.Persistence;
using Hospital.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Hospital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<HospitalDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AgentServiceOptions>(configuration.GetSection(AgentServiceOptions.SectionName));

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IStaffUserRepository, StaffUserRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUhidGenerator, SequentialUhidGenerator>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        var agentOptions = configuration.GetSection(AgentServiceOptions.SectionName).Get<AgentServiceOptions>()
            ?? new AgentServiceOptions();
        services.AddHttpClient<IAgentClient, AgentHttpClient>(client =>
        {
            client.BaseAddress = new Uri(agentOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
                };
            });

        services.AddAuthorization();
        return services;
    }
}
