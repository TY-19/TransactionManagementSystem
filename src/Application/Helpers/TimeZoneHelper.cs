using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Helpers;

public class TimeZoneHelper : ITimeZoneHelper
{
    private TimeZoneDetails? _timeZone;
    private TimeZoneInfo? _tzInfo;
    private int _stdOffsetSeconds = 0;
    private int? _dstOffsetSeconds = null;
    private readonly Dictionary<string, string> _knownTimeZoneAliases = new()
    {
        { "Europe/Kyiv", "Europe/Kiev" },
        { "Europe/Kiev", "Europe/Kyiv" }
    };

    private bool ResetTimeZone(TimeZoneDetails? userTimeZone)
    {
        if (userTimeZone == null)
        {
            if (_timeZone == null)
                return false;
            else
            {
                _timeZone = null;
                return true;
            }
        }
        else
        {
            if (userTimeZone.TimeZone == _timeZone?.TimeZone)
                return false;
            else
            {
                _timeZone = userTimeZone;
                return true;
            }
        }
    }
    public DateTime GetDateTime(DateTimeOffset dateTime, TimeZoneDetails? userTimeZone)
    {
        bool wasReset = ResetTimeZone(userTimeZone);
        if (_timeZone == null)
        {
            return dateTime.DateTime;
        }
        else
        {
            var offset = CalculateOffset(dateTime, wasReset);
            return dateTime.UtcDateTime.AddSeconds(offset);
        }
    }

    private TimeZoneInfo? GetTimeZoneInfo(bool update)
    {
        if (!update)
            return _tzInfo;

        if (_timeZone == null)
            _tzInfo = null;
        else if (TimeZoneInfo.TryFindSystemTimeZoneById(_timeZone.TimeZone, out var tz)
            || (_knownTimeZoneAliases.TryGetValue(_timeZone.TimeZone, out var alias)
                && TimeZoneInfo.TryFindSystemTimeZoneById(alias, out tz)))
            _tzInfo = tz;

        return _tzInfo;
    }

    private int CalculateOffset(DateTimeOffset dateTime, bool needRecalculation)
    {
        if (_timeZone == null)
            return (int)dateTime.Offset.TotalSeconds;

        if (needRecalculation)
        {
            _stdOffsetSeconds = _timeZone.StandardUtcOffsetSeconds;

            _dstOffsetSeconds = _timeZone.HasDayLightSaving
                ? _timeZone.DstOffsetToUtcSeconds
                : _stdOffsetSeconds;
        }
        var tz = GetTimeZoneInfo(needRecalculation);
        if (tz == null)
        {
            return RoughlyDetermineStandardOrDst(dateTime);
        }
        else
        {
            return tz.IsDaylightSavingTime(dateTime) && _dstOffsetSeconds.HasValue
                ? _dstOffsetSeconds.Value
                : _stdOffsetSeconds;
        }
    }
    private int RoughlyDetermineStandardOrDst(DateTimeOffset dateTime)
    {
        // log warning about possible error of using standard/dst time
        // so result will be approximate

        if (_timeZone == null)
            return _stdOffsetSeconds;

        int transactionYear = dateTime.Year;

        int dstStartMonth = _timeZone.DstStart?.Month ?? 3;
        int dstStartDay = _timeZone.DstStart?.Day ?? 20;
        int dstEndMonth = _timeZone.DstEnd?.Month ?? 10;
        int dstEndDay = _timeZone.DstEnd?.Day ?? 20;

        var dstApproxStart = new DateTimeOffset(transactionYear, dstStartMonth, dstStartDay, 3, 0, 0, TimeSpan.Zero);
        var dstApproxEnd = new DateTimeOffset(transactionYear, dstEndMonth, dstEndDay, 4, 0, 0, TimeSpan.Zero);

        return dateTime >= dstApproxStart && dateTime < dstApproxEnd && _dstOffsetSeconds.HasValue
            ? _dstOffsetSeconds.Value
            : _stdOffsetSeconds;
    }

    public int GetOffsetInSeconds(DateTimeOffset dateTime, TimeZoneDetails userTimeZone)
    {
        bool wasReset = ResetTimeZone(userTimeZone);
        return CalculateOffset(dateTime, wasReset);
    }

    public string GetReadableOffset(DateTimeOffset dateTime, TimeZoneDetails userTimeZone)
    {
        bool wasReset = ResetTimeZone(userTimeZone);
        var offset = CalculateOffset(dateTime, wasReset);

        char sign = offset >= 0 ? '+' : '-';
        int hours = Math.Abs(offset / 3600);
        int minutes = Math.Abs((offset % 3600) / 60);

        return $"{sign}{hours:D2}:{minutes:D2}";
    }
}
