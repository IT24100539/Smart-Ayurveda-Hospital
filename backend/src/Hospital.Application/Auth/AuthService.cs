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
    private readonly IEmailSender? _emailSender;
    private readonly ISmsSender? _smsSender;
    private readonly IClock? _clock;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly AccountLockoutOptions _lockout;
    private readonly PasswordPolicyOptions _passwordPolicy;

    private const int ResetTokenLifetimeMinutes = 30;

    public AuthService(
        IUserRepository users,
        IPatientRepository patients,
        IUhidGenerator uhid,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IUnitOfWork? unitOfWork = null,
        IEmailSender? emailSender = null,
        ISmsSender? smsSender = null,
        IClock? clock = null,
        AccountLockoutOptions? lockout = null,
        PasswordPolicyOptions? passwordPolicy = null)
    {
        _users = users;
        _patients = patients;
        _uhid = uhid;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _lockout = (lockout ?? new AccountLockoutOptions()).Normalized();
        _passwordPolicy = (passwordPolicy ?? new PasswordPolicyOptions()).Normalized();
    }

    private DateTimeOffset UtcNow => _clock?.UtcNow ?? DateTimeOffset.UtcNow;

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        UserRole? actorRole,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException(AuthMessages.RegistrationUnavailable);
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

        if (_unitOfWork is null)
        {
            throw new DomainException("Registration could not be saved. No patient record was opened.");
        }

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (role == UserRole.Patient)
            {
                await OpenPatientRecordAsync(user, request, ct);
            }

            await _users.AddAsync(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

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
            StampEmail(existingByEmail, user.Email);
            return;
        }

        var existingByPhone = await _patients.GetByPhoneAsync(user.PhoneNumber, cancellationToken);
        if (existingByPhone is not null)
        {
            if (string.IsNullOrWhiteSpace(existingByPhone.Email)
                || string.Equals(existingByPhone.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            {
                StampEmail(existingByPhone, user.Email);
                return;
            }

            throw new ConflictException(AuthMessages.RegistrationUnavailable);
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
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedException(AuthMessages.InvalidCredentials);
        }

        if (user.LockoutEnd is { } lockedUntil && lockedUntil > UtcNow)
        {
            throw new UnauthorizedException(AuthMessages.AccountLocked);
        }

        if (user.LockoutEnd is not null)
        {
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= _lockout.MaxFailedAttempts)
            {
                user.LockoutEnd = UtcNow.AddMinutes(_lockout.LockoutMinutes);
            }

            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedException(AuthMessages.InvalidCredentials);
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;

        // Heal missing email link: if the patient record was created by staff
        // (e.g., via phone number only) and has no email yet, stamp the login
        // email onto it so RequirePatientAsync can find it by email going forward.
        if (user.Role == UserRole.Patient)
        {
            await LinkPatientEmailIfMissingAsync(user, cancellationToken);
        }

        if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    public Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string resetBaseUrl,
        CancellationToken cancellationToken) =>
        RequestResetAsync(request, resetBaseUrl, cancellationToken);

    public Task<ResetPasswordResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken) =>
        CompleteResetAsync(request, cancellationToken);

    public async Task<ForgotPasswordResponse> RequestResetAsync(
        ForgotPasswordRequest request,
        string resetBaseUrl,
        CancellationToken cancellationToken)
    {
        const string genericMessage = "If an account with that email exists, a password reset link has been sent.";
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new ForgotPasswordResponse(genericMessage);
        }

        var rawToken = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = HashToken(rawToken);
        user.PasswordResetTokenExpiresAt = UtcNow.AddMinutes(ResetTokenLifetimeMinutes);

        if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (_emailSender != null)
        {
            await _emailSender.SendPasswordResetEmailAsync(user.Email, rawToken, resetBaseUrl, cancellationToken);
        }

        if (_smsSender != null && !string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            var link = $"{resetBaseUrl}?token={rawToken}&email={Uri.EscapeDataString(user.Email)}";
            await _smsSender.SendSmsAsync(
                user.PhoneNumber,
                $"Smart Ayurveda password reset: {link}",
                cancellationToken);
        }

        return new ForgotPasswordResponse(genericMessage);
    }

    public async Task<ResetPasswordResponse> CompleteResetAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordResetTokenHash))
        {
            throw new BadRequestException("Invalid or expired password reset token.");
        }

        if (!user.PasswordResetTokenExpiresAt.HasValue || user.PasswordResetTokenExpiresAt.Value < UtcNow)
        {
            RevokeResetToken(user);
            if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new BadRequestException("Invalid or expired password reset token.");
        }

        var tokenHash = HashToken(request.Token.Trim());
        if (!string.Equals(user.PasswordResetTokenHash, tokenHash, StringComparison.Ordinal))
        {
            throw new BadRequestException("Invalid or expired password reset token.");
        }

        EnsurePasswordMeetsPolicy(request.NewPassword);
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        RevokeResetToken(user);
        user.MustChangePassword = false;
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.TokenVersion++;

        if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse("Password has been reset successfully. You can now log in with your new password.");
    }

    private static void RevokeResetToken(User user)
    {
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAt = null;
    }

    private static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
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

        EnsurePasswordMeetsPolicy(request.NewPassword);

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

        if (_unitOfWork != null) await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            if (StampEmail(byEmail, user.Email) && _unitOfWork != null)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        // Look for a staff-created record matched by phone.
        var byPhone = await _patients.GetByPhoneAsync(user.PhoneNumber, cancellationToken);
        if (byPhone is not null
            && (string.IsNullOrWhiteSpace(byPhone.Email)
                || string.Equals(byPhone.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            && StampEmail(byPhone, user.Email)
            && _unitOfWork != null)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool StampEmail(Patient patient, string email)
    {
        if (string.Equals(patient.Email, email, StringComparison.Ordinal))
        {
            return false;
        }

        patient.Email = email;
        return true;
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = _jwt.Create(user);
        return new AuthResponse(
            token,
            expiresAt,
            new UserSummary(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role, user.MustChangePassword));
    }

    private void EnsurePasswordMeetsPolicy(string password)
    {
        var errors = PasswordRules.Evaluate(password, _passwordPolicy);
        if (errors.Count > 0)
        {
            throw new BadRequestException(string.Join(" ", errors));
        }
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
