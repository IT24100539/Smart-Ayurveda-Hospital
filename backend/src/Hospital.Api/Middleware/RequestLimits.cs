namespace Hospital.Api.Middleware;

public sealed class RequestTooLargeException : Exception
{
    public RequestTooLargeException()
        : base("The request body is too large.")
    {
    }
}

public sealed class RequestLimitsOptions
{
    public const string SectionName = "RequestLimits";

    public int MaxJsonBodyBytes { get; set; } = 1_048_576;
}
