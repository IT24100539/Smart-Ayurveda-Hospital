using System.Text.Json;

namespace Hospital.Api.Middleware;

/// <summary>
/// Fills empty 400, 404, and 413 responses with the same problem-details document used for exceptions.
/// </summary>
public sealed class EmptyErrorBodyMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;

    public EmptyErrorBodyMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.HasStarted || context.Response.ContentLength is > 0)
        {
            return;
        }

        if (context.Response.Body.CanSeek && context.Response.Body.Length > 0)
        {
            return;
        }

        var (type, title, detail) = context.Response.StatusCode switch
        {
            StatusCodes.Status400BadRequest => (
                "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                "Bad Request",
                "The request is invalid."),
            StatusCodes.Status404NotFound => (
                "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                "Not Found",
                "The requested resource was not found."),
            StatusCodes.Status413PayloadTooLarge => (
                "https://tools.ietf.org/html/rfc9110#section-15.5.14",
                "Payload Too Large",
                "The request body is too large."),
            _ => default
        };

        if (title is null)
        {
            return;
        }

        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type,
            title,
            status = context.Response.StatusCode,
            detail
        }, JsonOptions));
    }
}
