using System.Security.Claims;
using Hospital.Application.Abstractions;
using Hospital.Domain.Enums;
using Hospital.Domain.Exceptions;

namespace Hospital.Api.Security;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public HttpCurrentUser(IHttpContextAccessor http) => _http = http;

    public bool IsAuthenticated => _http.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirstValue("UserId")
                ?? _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(value, out var id))
            {
                throw new UnauthorizedException("Missing user id.");
            }

            return id;
        }
    }

    public string Email
    {
        get
        {
            var email = _http.HttpContext?.User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new UnauthorizedException("Missing email claim.");
            }

            return email;
        }
    }

    public UserRole Role
    {
        get
        {
            var value = _http.HttpContext?.User.FindFirstValue(ClaimTypes.Role);
            if (!Enum.TryParse<UserRole>(value, ignoreCase: true, out var role))
            {
                throw new UnauthorizedException("Missing role claim.");
            }

            return role;
        }
    }
}
