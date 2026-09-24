using FluentValidation;
using Hospital.Application.Abstractions;
using Hospital.Application.Appointments;
using Hospital.Application.Auth;
using Hospital.Application.Common;
using Hospital.Application.Communication;
using Hospital.Application.Patients;
using Microsoft.Extensions.DependencyInjection;

namespace Hospital.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IActorContext, ActorContext>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IInternalFeedbackService, InternalFeedbackService>();
        services.AddScoped<IReactionService, ReactionService>();
        services.AddScoped<IReplyService, ReplyService>();
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<INotificationService, NotificationService>();
        return services;
    }
}
