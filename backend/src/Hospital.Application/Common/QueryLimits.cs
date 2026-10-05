using FluentValidation;
using FluentValidation.Results;

namespace Hospital.Application.Common;

public static class QueryLimits
{
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 200;

    public static void EnsurePage(int page, int pageSize)
    {
        var failures = new List<ValidationFailure>();
        if (page < 1)
        {
            failures.Add(new ValidationFailure("page", "Page must be at least 1."));
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            failures.Add(new ValidationFailure("pageSize", $"Page size must be between 1 and {MaxPageSize}."));
        }

        ThrowIfAny(failures);
    }

    public static void EnsureLength(string propertyName, string? value, int max = MaxSearchLength)
    {
        if (value is not null && value.Length > max)
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(propertyName, $"{propertyName} must be at most {max} characters.")
            });
        }
    }

    public static void EnsureDateRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        var failures = new List<ValidationFailure>();
        if (from is { Year: < DateSanity.EarliestYear or > DateSanity.LatestYear })
        {
            failures.Add(new ValidationFailure("fromDate", "fromDate is outside the supported range."));
        }

        if (to is { Year: < DateSanity.EarliestYear or > DateSanity.LatestYear })
        {
            failures.Add(new ValidationFailure("toDate", "toDate is outside the supported range."));
        }

        if (from is not null && to is not null && to < from)
        {
            failures.Add(new ValidationFailure("toDate", "toDate must be on or after fromDate."));
        }

        ThrowIfAny(failures);
    }

    private static void ThrowIfAny(List<ValidationFailure> failures)
    {
        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }
    }
}
