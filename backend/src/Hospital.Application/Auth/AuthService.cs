using Hospital.Application.Abstractions;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Exceptions;

namespace Hospital.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IStaffUserRepository _staffUsers;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(IStaffUserRepository staffUsers, IPasswordHasher passwordHasher, IJwtTokenGenerator jwt)
    {
        _staffUsers = staffUsers;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _staffUsers.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new DomainException("Invalid email or password.");
        }

        var (token, expiresAt) = _jwt.Create(user);
        return new AuthResponse(token, expiresAt, user.Id, user.FullName, user.Email, user.Role);
    }
}
