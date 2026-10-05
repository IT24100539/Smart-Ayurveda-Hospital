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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUhidGenerator _uhid;

    public ActorContext(
        ICurrentUser current,
        IUserRepository users,
        IPatientRepository patients,
        IStaffUserRepository staff,
        IUnitOfWork unitOfWork,
        IUhidGenerator uhid)
    {
        _current = current;
        _users = users;
        _patients = patients;
        _staff = staff;
        _unitOfWork = unitOfWork;
        _uhid = uhid;
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

        // 1. Fast path: patient record has the login email already set.
        var patient = await _patients.GetByEmailAsync(user.Email, cancellationToken);
        if (patient is not null)
        {
            if (!string.Equals(patient.Email, user.Email, StringComparison.Ordinal))
            {
                patient.Email = user.Email.Trim().ToLowerInvariant();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return patient;
        }

        // 2. Healing path: patient record was staff-created by phone with no email or differing case.
        //    Stamp the login email now so every subsequent request uses the fast path.
        var loginEmail = user.Email.Trim().ToLowerInvariant();
        var byPhone = await _patients.GetByPhoneAsync(user.PhoneNumber, cancellationToken);
        if (byPhone is not null)
        {
            if (string.IsNullOrWhiteSpace(byPhone.Email)
                || string.Equals(byPhone.Email.Trim(), loginEmail, StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(byPhone.Email, loginEmail, StringComparison.Ordinal))
                {
                    byPhone.Email = loginEmail;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                return byPhone;
            }

            throw new DomainException(
                "No patient record is linked to this login. The clinical record must use the same email address.");
        }

        // 3. Legacy fallback: old rows where Patient.Id == User.Id.
        patient = await _patients.GetByIdAsync(user.Id, cancellationToken);
        if (patient is not null)
        {
            return patient;
        }

        // 4. Self-service: a patient login without a chart can still book visits.
        //    Open a UHID from the account so appointments and the health hub work.
        return await OpenChartFromLoginAsync(user, loginEmail, cancellationToken);
    }

    private async Task<Patient> OpenChartFromLoginAsync(
        User user,
        string loginEmail,
        CancellationToken cancellationToken)
    {
        var (firstName, lastName) = SplitName(user.FullName);
        var patient = new Patient
        {
            Uhid = await _uhid.NextAsync(cancellationToken),
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = new DateOnly(1900, 1, 1),
            Gender = Gender.Unspecified,
            Phone = string.IsNullOrWhiteSpace(user.PhoneNumber)
                ? $"0{user.Id:N}"[..10]
                : user.PhoneNumber.Trim(),
            Email = loginEmail,
            Prakriti = DoshaType.None,
            Vikriti = DoshaType.None,
            IsActive = true
        };

        await _patients.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return patient;
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
        if (trimmed.Length == 0)
        {
            return ("Patient", "Patient");
        }

        var space = trimmed.IndexOf(' ');
        if (space < 0)
        {
            return (Truncate(trimmed), Truncate(trimmed));
        }

        var first = trimmed[..space].Trim();
        var last = trimmed[(space + 1)..].Trim();
        if (last.Length == 0)
        {
            last = first;
        }

        return (Truncate(first), Truncate(last));
    }

    private static string Truncate(string value) => value.Length <= 80 ? value : value[..80];

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
