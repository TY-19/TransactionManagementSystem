namespace TMS.Application.Models;

/// <summary>
///     A date representation
/// </summary>
public class DateFilterParameters
{
    private bool isStartDate;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }

    private DateFilterParameters()
    { }

    /// <summary>
    ///   Returns a valid instance of the DateFilterParameters.
    /// </summary>
    /// <param name="year">Year of date. Cannot be null to create a valid date</param>
    /// <param name="month">Date month. Must be between 1 and 12</param>
    /// <param name="day">Date day. Must be between 1 and 31</param>
    /// <param name="isStartDate">If month and/or day are not provided then will use minimal
    ///     values if set to true or maximum values if set to false</param>
    /// <returns>
    ///     DateFilterParameters that contain a valid date
    ///     or null if valid date cannot be constructed from the parameters
    /// </returns>
    public static DateFilterParameters? CreateFilterParameters(int? year, int? month, int? day, bool isStartDate)
    {
        if (year == null)
            return null;

        DateFilterParameters dfp = new()
        {
            isStartDate = isStartDate,
            Year = year.Value,
            Month = GetDate(month, isStartDate, 1, 12)
        };
        dfp.Day = GetDate(day, isStartDate, 1, DateTime.DaysInMonth(dfp.Year, dfp.Month));

        return dfp;
    }

    private static int GetDate(int? value, bool isStartDate, int min, int max)
    {
        if (value != null && value >= min && value <= max)
            return value.Value;
        else
            return isStartDate ? min : max;
    }

    public DateTimeOffset AsOffset()
    {
        return isStartDate
            ? new DateTimeOffset(Year, Month, Day, 0, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(Year, Month, Day, 23, 59, 59, TimeSpan.Zero);
    }
}
