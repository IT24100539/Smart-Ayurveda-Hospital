using Hospital.Application.Auth.Dtos;

namespace Hospital.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
