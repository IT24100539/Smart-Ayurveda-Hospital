using Hospital.Application.Abstractions;
using Hospital.Application.Agents;
using Hospital.Infrastructure.Agents;
using Hospital.Infrastructure.Doctors;
using Hospital.Infrastructure.Documents;
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

        services.AddScoped<ClinicalAuditInterceptor>();
        services.AddDbContext<HospitalDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<ClinicalAuditInterceptor>()));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AgentServiceOptions>(configuration.GetSection(AgentServiceOptions.SectionName));
        services.Configure<DoctorPhotoOptions>(configuration.GetSection(DoctorPhotoOptions.SectionName));
        services.Configure<MedicalDocumentOptions>(configuration.GetSection(MedicalDocumentOptions.SectionName));

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
        services.AddScoped<IPatientDeviceTokenRepository, PatientDeviceTokenRepository>();
        services.AddScoped<IWorkflowExecutionRepository, WorkflowExecutionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddSingleton<IDoctorPhotoStore, DoctorPhotoStore>();
        services.AddScoped<IMedicalDocumentRepository, MedicalDocumentRepository>();
        // Development accepts every file. Replace IAntivirusScanner before production;
        // MedicalDocumentStore calls it after type and size checks and before writing bytes.
        services.AddSingleton<IAntivirusScanner, NoOpAntivirusScanner>();
        services.AddSingleton<IMedicalDocumentStore, MedicalDocumentStore>();
services.AddScoped<ITreatmentCatalog, TreatmentCatalog>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IUhidGenerator, SequentialUhidGenerator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSender, Services.DevEmailSender>();
        services.AddScoped<ISmsSender, Services.DevSmsSender>();
        services.AddScoped<IPushSender, Services.DevPushSender>();
        services.AddScoped<IAuditLogService, Services.AuditLogService>();

        var agentOptions = configuration.GetSection(AgentServiceOptions.SectionName).Get<AgentServiceOptions>()
            ?? new AgentServiceOptions();
        services.AddHttpClient<IAgentClient, AgentHttpClient>(client =>
        {
            client.BaseAddress = new Uri(agentOptions.BaseUrl);
            // llama3.1 classifies, then drafts. A cold model load plus those calls exceeds 60s.
            client.Timeout = TimeSpan.FromSeconds(180);
        });

        return services;
    }
}
