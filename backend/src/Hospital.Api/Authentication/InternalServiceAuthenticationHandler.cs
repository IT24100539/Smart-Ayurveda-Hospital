using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Hospital.Api.Authentication;

public sealed class InternalServiceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "InternalService";
    public const string PolicyName = "InternalServiceOnly";
    public const string HeaderName = "X-Internal-Service-Key";
    private readonly IConfiguration _configuration;

    public InternalServiceAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, IConfiguration configuration)
        : base(options, logger, encoder) => _configuration = configuration;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expected = _configuration["InternalService:Key"];
        if (string.IsNullOrWhiteSpace(expected) ||
            !Request.Headers.TryGetValue(HeaderName, out var values) || values.Count != 1 ||
            string.IsNullOrWhiteSpace(values[0]))
            return Task.FromResult(AuthenticateResult.Fail("Internal service authentication failed."));

        // Hash to equal-length buffers before constant-time comparison. Never log either key.
        var valid = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(values[0]!)));
        if (!valid)
            return Task.FromResult(AuthenticateResult.Fail("Internal service authentication failed."));

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "scheduling-agent") }, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
