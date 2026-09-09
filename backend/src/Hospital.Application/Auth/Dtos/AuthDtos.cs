using Hospital.Domain.Enums;

namespace Hospital.Application.Auth.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string FullName,
    string Email,
    StaffRole Role);
