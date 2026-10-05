using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Enums;

namespace Hospital.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, UserRole? actorRole, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task<ForgotPasswordResponse> RequestResetAsync(ForgotPasswordRequest request, string resetBaseUrl, CancellationToken cancellationToken);
    Task<ResetPasswordResponse> CompleteResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    [Obsolete("Use RequestResetAsync.")]
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string resetBaseUrl, CancellationToken cancellationToken)
        => RequestResetAsync(request, resetBaseUrl, cancellationToken);

    [Obsolete("Use CompleteResetAsync.")]
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
        => CompleteResetAsync(request, cancellationToken);
}
