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
/// Patient rows are matched on email until Member 1 adds Patient.UserId.
/// Staff moderation uses <see cref="StaffUser"/>, which is a different id from <see cref="User"/>.
/// </summary>
public interface IActorContext
{
    Task<User> RequireUserAsync(CancellationToken cancellationToken);
    Task<Patient> RequirePatientAsync(CancellationToken cancellationToken);
    Task<StaffUser> RequireStaffAsync(CancellationToken cancellationToken);
}
