﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Helpers;

public class TimeZoneHelper(
    IConfiguration _configuration,
    ILogger<TimeZoneHelper> _logger
    ) : ITimeZoneHelper
{
    private TimeZoneDetails? _timeZone;
    private TimeZoneInfo? _tzInfo;
    private int _stdOffsetSeconds = 0;
    private int? _dstOffsetSeconds = null;

    private readonly Dictionary<string, string> _knownTimeZoneAliases =
        _configuration.GetSection("TimeZoneAliases")?.Get<Dictionary<string, string>>() ?? [];

    /// <inheritdoc cref="ITimeZoneHelper.GetDateTime(DateTimeOffset, TimeZoneDetails?)"/>
    public DateTime GetDateTime(DateTimeOffset dateTime, TimeZoneDetails? userTimeZone)
    {
        bool wasReset = ResetTimeZone(userTimeZone);
        if (_timeZone == null)
            return dateTime.DateTime;
        
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

        char sign = offset >= 0 ? '+' : '-';
        int hours = Math.Abs(offset / 3600);
        int minutes = Math.Abs((offset % 3600) / 60);
        return $"{sign}{hours:D2}:{minutes:D2}";
    }

    private bool ResetTimeZone(TimeZoneDetails? userTimeZone)
    {
        if (userTimeZone?.TimeZone == _timeZone?.TimeZone)
            return false;

        _timeZone = userTimeZone;
        return true;
    }

    private int CalculateOffset(DateTimeOffset dateTime, bool needRecalculation)
    {
        if (_timeZone == null)
            return (int)dateTime.Offset.TotalSeconds;

        if (needRecalculation)
            RecalculateOffset();
        
        var tz = GetTimeZoneInfo(needRecalculation);
        if (tz == null)
            return RoughlyDetermineStandardOrDst(dateTime);
        
        return tz.IsDaylightSavingTime(dateTime) && _dstOffsetSeconds.HasValue
            ? _dstOffsetSeconds.Value
            : _stdOffsetSeconds;
    }

    private void RecalculateOffset()
    {
        if (_timeZone == null)
            return;
        
        _stdOffsetSeconds = _timeZone.StandardUtcOffsetSeconds;
        _dstOffsetSeconds = _timeZone.HasDayLightSaving
            ? _timeZone.DstOffsetToUtcSeconds
            : _stdOffsetSeconds;
    }
    private TimeZoneInfo? GetTimeZoneInfo(bool update)
    {
        if (!update)
            return _tzInfo;

        if (_timeZone == null)
        {
            _tzInfo = null;
            return _tzInfo;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(_timeZone.TimeZone, out var tz))
        {
            _tzInfo = tz;
        }
        else if (_knownTimeZoneAliases.TryGetValue(_timeZone.TimeZone, out var alias)
            && TimeZoneInfo.TryFindSystemTimeZoneById(alias, out tz))
        {
            _tzInfo = tz;
        }
        return _tzInfo;
    }
    private int RoughlyDetermineStandardOrDst(DateTimeOffset dateTime)
    {
        if (_timeZone == null)
            return _stdOffsetSeconds;

        _logger.LogWarning("DST rules cannot be resolved for the time zone {timezone}.", _timeZone.TimeZone);
        _logger.LogWarning("The approximate dates of starting and ending of the daylight saving time will be used.");

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
}