using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Hospital.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Hospital.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
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
        var (status, title, type, detail, errors) = Map(exception);

        if (status == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(exception, "Request failed with {Status}", status);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            type,
            title,
            status = (int)status,
            detail,
            errors
        }, JsonOptions));
    }

    private (HttpStatusCode Status, string Title, string Type, string Detail, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception exception)
    {
        switch (exception)
        {
            case ValidationException validation:
                var errors = validation.Errors
                    .GroupBy(error => string.IsNullOrWhiteSpace(error.PropertyName) ? "_error" : error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => ProblemDetailSanitizer.PublicMessage(error.ErrorMessage, "The value is invalid.")).Distinct().ToArray());
                return (
                    HttpStatusCode.BadRequest,
                    "One or more validation errors occurred.",
                    "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    "The request is invalid.",
                    errors);
            case RequestTooLargeException:
                return Problem(HttpStatusCode.RequestEntityTooLarge, "Payload Too Large", "https://tools.ietf.org/html/rfc9110#section-15.5.14", "The request body is too large.");
            case BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge:
                return Problem(HttpStatusCode.RequestEntityTooLarge, "Payload Too Large", "https://tools.ietf.org/html/rfc9110#section-15.5.14", "The request body is too large.");
            case BadHttpRequestException:
            case JsonException:
                return Problem(HttpStatusCode.BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1", "The request body is not valid JSON.");
            case UnauthorizedException:
                return Problem(HttpStatusCode.Unauthorized, "Unauthorized", "https://tools.ietf.org/html/rfc9110#section-15.5.2", Safe(exception.Message, "Authentication is required."));
            case ForbiddenException:
                return Problem(HttpStatusCode.Forbidden, "Forbidden", "https://tools.ietf.org/html/rfc9110#section-15.5.4", Safe(exception.Message, "You are not allowed to perform this action."));
            case NotFoundException:
                return Problem(HttpStatusCode.NotFound, "Not Found", "https://tools.ietf.org/html/rfc9110#section-15.5.5", Safe(exception.Message, "The requested resource was not found."));
            case ConflictException:
                return Problem(HttpStatusCode.Conflict, "Conflict", "https://tools.ietf.org/html/rfc9110#section-15.5.10", Safe(exception.Message, "The request conflicts with the current state."));
            case InvalidScheduleException:
                return Problem(HttpStatusCode.BadRequest, "Invalid schedule", "https://tools.ietf.org/html/rfc9110#section-15.5.1", Safe(exception.Message, "The schedule is invalid."));
            case DomainException:
                return Problem(HttpStatusCode.BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1", Safe(exception.Message, "The request is invalid."));
            case HttpRequestException:
                return Problem(HttpStatusCode.BadGateway, "Bad Gateway", "https://tools.ietf.org/html/rfc9110#section-15.6.3", "The internal agent service is unavailable.");
            default:
                return Problem(
                    HttpStatusCode.InternalServerError,
                    "An unexpected error occurred.",
                    "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                    _environment.IsProduction()
                        ? "An unexpected error occurred."
                        : Safe(null, "An unexpected error occurred."));
        }
    }

    private static (HttpStatusCode Status, string Title, string Type, string Detail, IReadOnlyDictionary<string, string[]>? Errors) Problem(
        HttpStatusCode status,
        string title,
        string type,
        string detail) => (status, title, type, detail, null);

    private string Safe(string? message, string fallback) =>
        _environment.IsProduction()
            ? ProblemDetailSanitizer.PublicMessage(message, fallback)
            : ProblemDetailSanitizer.PublicMessage(message, fallback);
}
