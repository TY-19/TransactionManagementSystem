namespace TMS.Application.Models;

public class DateFilterParameters
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }
    private DateFilterParameters()
    { }
    public static DateFilterParameters? CreateFilterParameters(int? year, int? month, int? day, bool isStartDate)
    {
        if (year == null)
            return null;

        DateFilterParameters dfp = new()
        {
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
}
