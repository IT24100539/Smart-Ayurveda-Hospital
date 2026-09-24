using System.Net;
using System.Text.Json;
using FluentValidation;
using Hospital.Domain.Exceptions;

namespace Hospital.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync(context, ex);
        }
    }

    private async Task WriteAsync(HttpContext context, Exception exception)
    {
        object body;
        HttpStatusCode status;

        switch (exception)
        {
            case ValidationException validation:
                status = HttpStatusCode.BadRequest;
                body = new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "One or more validation errors occurred.",
                    status = (int)status,
                    errors = validation.Errors
                        .GroupBy(e => string.IsNullOrWhiteSpace(e.PropertyName) ? "_error" : e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                };
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            case UnauthorizedException:
                status = HttpStatusCode.Unauthorized;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.5.2", "Unauthorized", status, exception.Message);
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            case NotFoundException:
                status = HttpStatusCode.NotFound;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.5.5", "Not Found", status, exception.Message);
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            case ConflictException:
                status = HttpStatusCode.Conflict;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.5.10", "Conflict", status, exception.Message);
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            case InvalidScheduleException:
                status = HttpStatusCode.BadRequest;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.5.1", "Invalid schedule", status, exception.Message);
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            case DomainException:
                status = HttpStatusCode.BadRequest;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.5.1", "Bad Request", status, exception.Message);
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            case HttpRequestException:
                status = HttpStatusCode.BadGateway;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.6.3", "Bad Gateway", status, "The internal agent service is unavailable.");
                _logger.LogWarning(exception, "Request failed with {Status}", status);
                break;
            default:
                status = HttpStatusCode.InternalServerError;
                body = Problem("https://tools.ietf.org/html/rfc9110#section-15.6.1", "An unexpected error occurred.", status, "An unexpected error occurred.");
                _logger.LogError(exception, "Unhandled exception");
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }

    private static object Problem(string type, string title, HttpStatusCode status, string detail) => new
    {
        type,
        title,
        status = (int)status,
        detail
    };
}
