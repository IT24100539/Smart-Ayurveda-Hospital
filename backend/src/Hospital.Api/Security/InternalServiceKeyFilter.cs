using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Hospital.Api.Security;

/// <summary>
/// Shared secret for agent-to-API calls. Mirrors the Prompt 1.7 internal guard:
/// header <c>X-Internal-Service-Key</c>, not a staff or patient JWT.
/// </summary>
public sealed class InternalServiceOptions
{
    public const string SectionName = "InternalService";

    public string ApiKey { get; set; } = string.Empty;
}

public sealed class InternalServiceKeyFilter : IAsyncActionFilter
{
    public const string HeaderName = "X-Internal-Service-Key";

    private readonly InternalServiceOptions _options;

    public InternalServiceKeyFilter(IOptions<InternalServiceOptions> options) => _options = options.Value;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!KeysMatch(provided, _options.ApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new { detail = "Invalid internal service key." });
            return;
        }

        await next();
    }

    private static bool KeysMatch(string provided, string expected)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
