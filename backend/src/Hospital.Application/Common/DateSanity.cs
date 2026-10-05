namespace Hospital.Application.Common;

public static class DateSanity
{
    public const int EarliestYear = 1900;
    public const int LatestYear = 2100;

    public static bool IsCalendarDate(DateOnly date) =>
        date != default && date.Year is >= EarliestYear and <= LatestYear;

    public static bool IsBirthDate(DateOnly date)
    {
        if (!IsCalendarDate(date))
        {
            return false;
        }

        return date <= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
