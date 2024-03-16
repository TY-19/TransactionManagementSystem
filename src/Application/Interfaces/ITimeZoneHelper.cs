using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITimeZoneHelper
{
    /// <summary>
    ///     Returns date time at the specified moment in the specified timezone.
    /// </summary>
    /// <param name="dateTime">Date to transform</param>
    /// <param name="userTimeZone">Time zone to calculate offset</param>
    /// <returns>Date time in time of the specified timezone or its own timezone if
    ///     user time zone was not specified.
    /// </returns>
    DateTime GetDateTime(DateTimeOffset dateTime, TimeZoneDetails? userTimeZone);
    /// <summary>
    ///     Returns an offset in seconds
    /// </summary>
    /// <param name="dateTime">Date time to get offset of.</param>
    /// <param name="userTimeZone">Time zone to calculate offset.</param>
    /// <returns>Offset in seconds</returns>
    int GetOffsetInSeconds(DateTimeOffset dateTime, TimeZoneDetails userTimeZone);
    /// <summary>
    ///     Returns an offset in format '+00:00'
    /// </summary>
    /// <param name="dateTime">Date time to get offset of.</param>
    /// <param name="userTimeZone">Time zone to calculate offset.</param>
    /// <returns>Offset in format '+00:00'</returns>
    string GetReadableOffset(DateTimeOffset dateTime, TimeZoneDetails userTimeZone);
}
