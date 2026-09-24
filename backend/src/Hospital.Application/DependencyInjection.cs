using FluentValidation;
using Hospital.Application.Appointments;
using Hospital.Application.Auth;
using Hospital.Application.Patients;
using Hospital.Application.Wards;
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
        services.AddScoped<IWardService, WardService>();
        services.AddScoped<IBookingValidator, BookingValidator>();
        services.AddScoped<TreatmentAvailabilityService>();
        return services;
    }
}
