using Hospital.Application.Abstractions;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IUnitOfWork unitOfWork)
    {
        _users = users;
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

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role,
            IsActive = true
        };

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = _jwt.Create(user);
        return new AuthResponse(
            token,
            expiresAt,
            new UserSummary(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role));
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
