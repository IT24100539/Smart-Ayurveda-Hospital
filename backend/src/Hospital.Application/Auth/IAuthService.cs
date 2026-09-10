using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, UserRole? actorRole, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
