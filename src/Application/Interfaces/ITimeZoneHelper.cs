using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITimeZoneHelper
{
    /// <summary>
    /// Returns the date and time at the specified moment in the specified time zone.
    /// </summary>
    /// <param name="dateTime">The date to transform.</param>
    /// <param name="userTimeZone">The time zone to calculate the offset.</param>
    /// <returns>
    /// The date and time in the specified time zone, or its own time zone if the user time zone 
    /// was not specified.
    /// </returns>
    DateTime GetDateTime(DateTimeOffset dateTime, TimeZoneDetails? userTimeZone);

    /// <summary>
    /// Returns the offset in seconds.
    /// </summary>
    /// <param name="dateTime">The date and time to get the offset of.</param>
    /// <param name="userTimeZone">The time zone to calculate the offset.</param>
    /// <returns>The offset in seconds.</returns>
    int GetOffsetInSeconds(DateTimeOffset dateTime, TimeZoneDetails userTimeZone);

    /// <summary>
    /// Get offset in the format '+00:00'.
    /// </summary>
    /// <param name="dateTime">The date and time to get the offset of.</param>
    /// <param name="userTimeZone">The time zone to calculate the offset.</param>
    /// <returns>The offset in the format '+00:00'.</returns>
    string GetReadableOffset(DateTimeOffset dateTime, TimeZoneDetails userTimeZone);
}
