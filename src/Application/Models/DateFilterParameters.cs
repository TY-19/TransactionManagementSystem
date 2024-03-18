namespace TMS.Application.Models;

/// <summary>
/// A date representation designed to store the lower or higher limit of the time interval.
/// </summary>
public class DateFilterParameters
{
    private bool _isStartDate;
    public bool IsStartDate => _isStartDate;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }

    private DateFilterParameters()
    { }

    /// <summary>
    /// Returns a valid instance of the DateFilterParameters.
    /// </summary>
    /// <param name="year">The year of the date. Cannot be null to create a valid date.</param>
    /// <param name="month">The month of the date. Must be between 1 and 12.</param>
    /// <param name="day">The day of the date. Must be between 1 and 31.</param>
    /// <param name="isStartDate">
    /// If month and/or day are not provided, this parameter determines whether to use minimal
    /// values (if set to true) or maximum values (if set to false).
    /// </param>
    /// <returns>
    /// A DateFilterParameters object that contains a valid date,
    /// or null if a valid date cannot be constructed from the parameters.
    /// </returns>
    public static DateFilterParameters? CreateFilterParameters(
        int? year, int? month, int? day, bool isStartDate)
    {
        if (year == null)
        {
            return null;
        }

        DateFilterParameters dfp = new()
        {
            _isStartDate = isStartDate,
            Year = year.Value,
            Month = GetDateInValidRange(month, isStartDate, 1, 12)
        };
        dfp.Day = GetDateInValidRange(day, isStartDate, 1, DateTime.DaysInMonth(dfp.Year, dfp.Month));

        return dfp;
    }

    private static int GetDateInValidRange(int? value, bool isStartDate, int min, int max)
    {
        if (value != null && value >= min && value <= max)
        {
            return value.Value;
        }
        else
        {
            return isStartDate ? min : max;
        }
    }

    /// <summary>
    /// Return an offset representation of the object.
    /// </summary>
    /// <returns>A <see cref="DateTimeOffset"/> representation of the <see cref="DateFilterParameters"/>.</returns>
    public DateTimeOffset AsOffset()
    {
        return _isStartDate
            ? new DateTimeOffset(Year, Month, Day, 0, 0, 0, TimeSpan.Zero)
            : new DateTimeOffset(Year, Month, Day, 23, 59, 59, TimeSpan.Zero);
    }
}
