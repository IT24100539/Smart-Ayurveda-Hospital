using Hospital.Application.Abstractions;
using Hospital.Domain.Entities;
using Hospital.Domain.Enums;

namespace Hospital.Api.Security;

/// <summary>
/// Scoped stand-in for the JWT caller. The complaint escalation worker sets this
/// so application services can run without an HTTP request.
/// </summary>
public sealed class CurrentUserOverride
{
    public ICurrentUser? User { get; set; }
}

public sealed class CompositeCurrentUser : ICurrentUser
{
    private readonly CurrentUserOverride _override;
    private readonly HttpCurrentUser _http;

    public CompositeCurrentUser(CurrentUserOverride currentOverride, HttpCurrentUser http)
    {
        _override = currentOverride;
        _http = http;
    }

    private ICurrentUser Active => _override.User ?? _http;

    public bool IsAuthenticated => Active.IsAuthenticated;
    public Guid UserId => Active.UserId;
    public string Email => Active.Email;
    public UserRole Role => Active.Role;
}

public sealed class FixedCurrentUser : ICurrentUser
{
    private readonly User _user;

    public FixedCurrentUser(User user) => _user = user;

    public bool IsAuthenticated => _user.IsActive;
    public Guid UserId => _user.Id;
    public string Email => _user.Email;
    public UserRole Role => _user.Role;
}
