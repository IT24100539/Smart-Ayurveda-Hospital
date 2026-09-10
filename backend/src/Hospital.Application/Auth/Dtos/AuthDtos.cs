using Hospital.Domain.Enums;

namespace Hospital.Application.Auth.Dtos;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    UserRole? Role = null);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserSummary(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    UserRole Role);

public sealed record AuthResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    UserSummary User);
