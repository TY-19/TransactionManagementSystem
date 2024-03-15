using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITimeZoneHelper
{
    DateTime GetDateTime(DateTimeOffset dateTime, TimeZoneDetails? userTimeZone);
    int GetOffsetInSeconds(DateTimeOffset dateTime, TimeZoneDetails userTimeZone);
    string GetReadableOffset(DateTimeOffset dateTime, TimeZoneDetails userTimeZone);
}
