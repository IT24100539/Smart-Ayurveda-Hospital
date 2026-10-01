using Hospital.Application.Abstractions;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPatientRepository _patients;
    private readonly IUhidGenerator _uhid;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository users,
        IPatientRepository patients,
        IUhidGenerator uhid,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _patients = patients;
        _uhid = uhid;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        UserRole? actorRole,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"An account with email '{email}' already exists.");
        }

        var role = UserRole.Patient;
        if (actorRole == UserRole.Admin && request.Role is { } requestedRole)
        {
            role = requestedRole;
        }

        var phone = request.PhoneNumber.Trim();
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = phone,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true
        };

        if (role == UserRole.Patient)
        {
            await OpenPatientRecordAsync(user, request, cancellationToken);
        }

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    /// <summary>
    /// A login is not a hospital chart. Self-registration opens the clinical
    /// record (UHID) on the same email so later visits can find this patient.
    /// </summary>
    private async Task OpenPatientRecordAsync(User user, RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request.DateOfBirth is not { } dateOfBirth || request.Gender is not { } gender)
        {
            throw new DomainException("Date of birth and gender are required to open a patient record.");
        }

        var existingByEmail = await _patients.GetByEmailAsync(user.Email, cancellationToken);
        if (existingByEmail is not null)
        {
            return;
        }

        var existingByPhone = await _patients.GetByPhoneAsync(user.PhoneNumber, cancellationToken);
        if (existingByPhone is not null)
        {
            if (string.IsNullOrWhiteSpace(existingByPhone.Email))
            {
                existingByPhone.Email = user.Email;
                return;
            }

            if (string.Equals(existingByPhone.Email, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            throw new ConflictException(
                $"A patient record already uses phone '{user.PhoneNumber}' ({existingByPhone.Uhid}).");
        }

        var (firstName, lastName) = SplitName(user.FullName);
        var patient = new Patient
        {
            Uhid = await _uhid.NextAsync(cancellationToken),
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            Gender = gender,
            Phone = user.PhoneNumber,
            Email = user.Email,
            Prakriti = DoshaType.None,
            Vikriti = DoshaType.None,
            IsActive = true
        };

        await _patients.AddAsync(patient, cancellationToken);
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
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

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Heal missing email link: if the patient record was created by staff
        // (e.g., via phone number only) and has no email yet, stamp the login
        // email onto it so RequirePatientAsync can find it by email going forward.
        if (user.Role == UserRole.Patient)
        {
            await LinkPatientEmailIfMissingAsync(user, cancellationToken);
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedException("User not found.");

        if (!user.IsActive)
        {
            throw new UnauthorizedException("User account is inactive.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new BadRequestException("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new BadRequestException("New password must be at least 8 characters long.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new BadRequestException("New password and confirmation do not match.");
        }

        if (request.CurrentPassword == request.NewPassword)
        {
            throw new BadRequestException("New password cannot be the same as the current password.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.TokenVersion++;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    /// <summary>
    /// Silently links the login email to an existing patient record that was
    /// staff-created by phone (no email on the patient row). No-ops when the
    /// patient record already has the correct email or doesn't exist at all.
    /// </summary>
    private async Task LinkPatientEmailIfMissingAsync(User user, CancellationToken cancellationToken)
    {
        // If a record already matches by email, nothing to do.
        var byEmail = await _patients.GetByEmailAsync(user.Email, cancellationToken);
        if (byEmail is not null)
        {
            return;
        }

        // Look for a staff-created record matched by phone.
        var byPhone = await _patients.GetByPhoneAsync(user.PhoneNumber, cancellationToken);
        if (byPhone is not null && string.IsNullOrWhiteSpace(byPhone.Email))
        {
            byPhone.Email = user.Email;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = _jwt.Create(user);
        return new AuthResponse(
            token,
            expiresAt,
            new UserSummary(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role, user.MustChangePassword));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
