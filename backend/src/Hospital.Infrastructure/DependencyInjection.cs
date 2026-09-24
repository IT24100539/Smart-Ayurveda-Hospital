using Hospital.Application.Abstractions;
using Hospital.Application.Agents;
using Hospital.Infrastructure.Agents;
using Hospital.Infrastructure.Identity;
using Hospital.Infrastructure.Persistence;
using Hospital.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<HospitalDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AgentServiceOptions>(configuration.GetSection(AgentServiceOptions.SectionName));

        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IStaffUserRepository, StaffUserRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITreatmentRepository, TreatmentRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
services.AddScoped<ITreatmentRepository, TreatmentRepository>();
services.AddScoped<IWardRepository, WardRepository>();
services.AddScoped<IFeedbackRepository, FeedbackRepository>();
services.AddScoped<IFeedbackReactionRepository, FeedbackReactionRepository>();
services.AddScoped<IFeedbackReplyRepository, FeedbackReplyRepository>();
services.AddScoped<IComplaintRepository, ComplaintRepository>();
services.AddScoped<INotificationRepository, NotificationRepository>();
services.AddScoped<ITreatmentCatalog, TreatmentCatalog>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUhidGenerator, SequentialUhidGenerator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        var agentOptions = configuration.GetSection(AgentServiceOptions.SectionName).Get<AgentServiceOptions>()
            ?? new AgentServiceOptions();
        services.AddHttpClient<IAgentClient, AgentHttpClient>(client =>
        {
            client.BaseAddress = new Uri(agentOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}
