using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Common;

public sealed class ActorContext : IActorContext
{
    private readonly ICurrentUser _current;
    private readonly IUserRepository _users;
    private readonly IPatientRepository _patients;
    private readonly IStaffUserRepository _staff;

    public ActorContext(
        ICurrentUser current,
        IUserRepository users,
        IPatientRepository patients,
        IStaffUserRepository staff)
    {
        _current = current;
        _users = users;
        _patients = patients;
        _staff = staff;
    }

    public async Task<User> RequireUserAsync(CancellationToken cancellationToken)
    {
        if (!_current.IsAuthenticated)
        {
            throw new UnauthorizedException("Sign in is required.");
        }

        var user = await _users.GetByIdAsync(_current.UserId, cancellationToken)
            ?? throw new UnauthorizedException("Account was not found.");
        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is inactive.");
        }

        return user;
    }

    public async Task<Patient> RequirePatientAsync(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user.Role != UserRole.Patient)
        {
            throw new ForbiddenException("Only a patient can perform this action.");
        }

        var patient = await _patients.GetByEmailAsync(user.Email, cancellationToken)
            ?? throw new DomainException(
                "No patient record is linked to this login. The clinical record must use the same email address.");
        return patient;
    }

    public async Task<StaffUser> RequireStaffAsync(CancellationToken cancellationToken)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user.Role is not (UserRole.FrontDeskStaff or UserRole.Doctor or UserRole.Admin))
        {
            throw new ForbiddenException("Only staff can perform this action.");
        }

        var staff = await _staff.GetByEmailAsync(user.Email, cancellationToken)
            ?? throw new DomainException("No staff profile is linked to this login.");
        return staff;
    }
}
