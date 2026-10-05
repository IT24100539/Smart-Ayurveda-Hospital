using Hospital.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace Hospital.Api.Middleware;

public sealed class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly int _limit;
    private readonly long _documentLimit;

    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        IOptions<RequestLimitsOptions> options,
        IOptions<MedicalDocumentOptions> documents)
    {
        _next = next;
        var configured = options.Value.MaxJsonBodyBytes;
        _limit = configured < 256 ? 256 : configured;
        _documentLimit = (long)documents.Value.MaxBytes + MedicalDocumentOptions.MultipartOverheadBytes;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var limit = IsDocumentUpload(context) ? _documentLimit : _limit;
        if (context.Request.ContentLength is long length && length > limit)
        {
            throw new RequestTooLargeException();
        }

        await _next(context);
    }

    private static bool IsDocumentUpload(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path.Value;
        return path is not null
            && (path.Equals("/api/medical-documents", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/api/medical-documents/", StringComparison.OrdinalIgnoreCase));
    }
}
