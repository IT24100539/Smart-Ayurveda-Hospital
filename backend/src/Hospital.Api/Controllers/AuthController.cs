using FluentValidation;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<LoginRequest> _validator;

    public AuthController(IAuthService auth, IValidator<LoginRequest> validator)
    {
        _auth = auth;
        _validator = validator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _auth.LoginAsync(request, cancellationToken));
    }
}
