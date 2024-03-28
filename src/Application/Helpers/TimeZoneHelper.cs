using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Helpers;

public class TimeZoneHelper(
    IConfiguration configuration,
    ILogger<TimeZoneHelper> logger
    ) : ITimeZoneHelper
{
    private TimeZoneDetails? _timeZone;
    private TimeZoneInfo? _tzInfo;
    private int _stdOffsetSeconds;
    private int? _dstOffsetSeconds = null;

    private readonly Dictionary<string, string> _knownTimeZoneAliases =
        configuration.GetSection("TimeZoneAliases")?.Get<Dictionary<string, string>>() ?? [];

    /// <inheritdoc cref="ITimeZoneHelper.GetDateTime(DateTimeOffset, TimeZoneDetails?)"/>
    public DateTime GetDateTime(DateTimeOffset dateTime, TimeZoneDetails? userTimeZone)
    {
        bool wasReset = ResetTimeZone(userTimeZone);
        if (_timeZone == null)
        {
            return dateTime.DateTime;
        }
        int offset = CalculateOffset(dateTime, wasReset);
        return dateTime.UtcDateTime.AddSeconds(offset);
    }
    /// <inheritdoc cref="ITimeZoneHelper.GetOffsetInSeconds(DateTimeOffset, TimeZoneDetails)"/>
    public int GetOffsetInSeconds(DateTimeOffset dateTime, TimeZoneDetails userTimeZone)
    {
        bool wasReset = ResetTimeZone(userTimeZone);
        return CalculateOffset(dateTime, wasReset);
    }
    /// <inheritdoc cref="ITimeZoneHelper.GetReadableOffset(DateTimeOffset, TimeZoneDetails)"/>
    public string GetReadableOffset(DateTimeOffset dateTime, TimeZoneDetails userTimeZone)
    {
        int offset = GetOffsetInSeconds(dateTime, userTimeZone);
        return GetReadableOffset(offset);
    }
    /// <inheritdoc cref="ITimeZoneHelper.GetReadableOffset(int)"/>
    public string GetReadableOffset(int offsetSeconds)
    {
        char sign = offsetSeconds >= 0 ? '+' : '-';
        int hours = Math.Abs(offsetSeconds / 3600);
        int minutes = Math.Abs((offsetSeconds % 3600) / 60);
        return $"{sign}{hours:D2}:{minutes:D2}";
    }

    private bool ResetTimeZone(TimeZoneDetails? userTimeZone)
    {
        if (userTimeZone?.TimeZoneName == _timeZone?.TimeZoneName)
        {
            return false;
        }
        _timeZone = userTimeZone;
        return true;
    }

    private int CalculateOffset(DateTimeOffset dateTime, bool needRecalculation)
    {
        if (_timeZone == null)
        {
            return (int)dateTime.Offset.TotalSeconds;
        }
        if (needRecalculation)
        {
            RecalculateOffset();
        }

        TimeZoneInfo? tzInfo = GetTimeZoneInfo(needRecalculation);
        if (tzInfo == null)
        {
            return RoughlyDetermineStandardOrDst(dateTime);
        }

        return tzInfo.IsDaylightSavingTime(dateTime) && _dstOffsetSeconds.HasValue
            ? _dstOffsetSeconds.Value
            : _stdOffsetSeconds;
    }

    private void RecalculateOffset()
    {
        if (_timeZone != null)
        {
            _stdOffsetSeconds = _timeZone.StandardUtcOffsetSeconds;
            _dstOffsetSeconds = _timeZone.HasDayLightSaving
                ? _timeZone.DstOffsetToUtcSeconds
                : _stdOffsetSeconds;
        }
    }
    private TimeZoneInfo? GetTimeZoneInfo(bool wasChanged)
    {
        if (!wasChanged)
        {
            return _tzInfo;
        }

        if (_timeZone == null)
        {
            _tzInfo = null;
            return _tzInfo;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(_timeZone.TimeZoneName, out TimeZoneInfo? tzInfo))
        {
            _tzInfo = tzInfo;
        }
        else if (_knownTimeZoneAliases.TryGetValue(_timeZone.TimeZoneName, out string? alias)
            && TimeZoneInfo.TryFindSystemTimeZoneById(alias, out tzInfo))
        {
            _tzInfo = tzInfo;
        }
        return _tzInfo;
    }
    private int RoughlyDetermineStandardOrDst(DateTimeOffset dateTime)
    {
        if (_timeZone == null || !_timeZone.HasDayLightSaving
            || _timeZone.DstStart == null || _timeZone.DstEnd == null)
        {
            return _stdOffsetSeconds;
        }

        logger.LogWarning("DST rules cannot be resolved for the time zone {timezone}.", _timeZone.TimeZoneName);
        logger.LogWarning("The approximate dates for the start and end of daylight saving time will be used.");

        int transactionYear = dateTime.Year;

        int dstStartMonth = _timeZone.DstStart.Value.Month;
        int dstStartDay = _timeZone.DstStart.Value.Day;
        int dstEndMonth = _timeZone.DstEnd.Value.Month;
        int dstEndDay = _timeZone.DstEnd.Value.Day;

        var dstApproxStart = new DateTimeOffset(transactionYear, dstStartMonth, dstStartDay,
            3, 0, 0, TimeSpan.FromSeconds(_stdOffsetSeconds));
        var dstApproxEnd = new DateTimeOffset(transactionYear, dstEndMonth, dstEndDay,
            4, 0, 0, TimeSpan.FromSeconds(_dstOffsetSeconds ?? _stdOffsetSeconds));

        return dateTime >= dstApproxStart && dateTime < dstApproxEnd && _dstOffsetSeconds.HasValue
            ? _dstOffsetSeconds.Value
            : _stdOffsetSeconds;
    }
}
