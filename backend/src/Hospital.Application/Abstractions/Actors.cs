using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Application.Abstractions;

/// <summary>
/// Authenticated caller, resolved from the JWT. Implemented in the API host.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    string Email { get; }
    UserRole Role { get; }
}

/// <summary>
/// Loads the clinical patient or staff profile linked to the signed-in account.
/// <see cref="Patient.Id"/> is the chart id. It is not the JWT user id.
/// Staff moderation uses <see cref="StaffUser"/>, which is also a different id from <see cref="User"/>.
/// </summary>
public interface IActorContext
{
    Task<User> RequireUserAsync(CancellationToken cancellationToken);
    Task<Patient> RequirePatientAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Chart id (<c>patients.Id</c>) for the signed-in patient. Never the JWT user id.
    /// Feedback, complaints, and notifications all filter on this value.
    /// </summary>
    async Task<Guid> RequirePatientIdAsync(CancellationToken cancellationToken)
    {
        var patient = await RequirePatientAsync(cancellationToken);
        return patient.Id;
    }

    Task<StaffUser> RequireStaffAsync(CancellationToken cancellationToken);
}
