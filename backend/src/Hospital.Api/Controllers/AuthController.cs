using System.Security.Claims;
using FluentValidation;
using Hospital.Api.Security;
using Hospital.Application.Auth;
using Hospital.Application.Auth.Dtos;
using Hospital.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Hospital.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public AuthController(
        IAuthService auth,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<ForgotPasswordRequest> forgotValidator,
        IValidator<ResetPasswordRequest> resetValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _auth = auth;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _forgotValidator = forgotValidator;
        _resetValidator = resetValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        await _registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _auth.RegisterAsync(request, GetActorRole(), cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await _loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _auth.LoginAsync(request, cancellationToken));
    }

    [HttpPost("request-reset")]
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ForgotPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ForgotPasswordResponse>> RequestReset(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _forgotValidator.ValidateAndThrowAsync(request, cancellationToken);
        var baseUrl = $"{Request.Scheme}://{Request.Host}/reset-password";
        return Ok(await _auth.RequestResetAsync(request, baseUrl, cancellationToken));
    }

    [HttpPost("complete-reset")]
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(AuthRateLimitOptions.PolicyName)]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResetPasswordResponse>> CompleteReset(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _resetValidator.ValidateAndThrowAsync(request, cancellationToken);
        return Ok(await _auth.CompleteResetAsync(request, cancellationToken));
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        await _changePasswordValidator.ValidateAndThrowAsync(request, cancellationToken);
        var response = await _auth.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    private UserRole? GetActorRole()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var value = User.FindFirstValue(ClaimTypes.Role);
        return Enum.TryParse<UserRole>(value, ignoreCase: true, out var role) ? role : null;
    }
}
