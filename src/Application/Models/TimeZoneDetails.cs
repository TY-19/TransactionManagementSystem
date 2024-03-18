namespace TMS.Application.Models;

/// <summary>
/// Represents details about a time zone.
/// </summary>
public class TimeZoneDetails
{
    /// <summary>
    /// Gets or sets the name of the time zone.
    /// </summary>
    public string TimeZoneName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the standard UTC offset of the time zone in seconds.
    /// </summary>
    public int StandardUtcOffsetSeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the time zone has daylight saving time.
    /// </summary>
    public bool HasDayLightSaving { get; set; }

    /// <summary>
    /// Gets or sets the offset to UTC during daylight saving time in seconds, if applicable.
    /// </summary>
    public int? DstOffsetToUtcSeconds { get; set; }

    /// <summary>
    /// Gets or sets the offset to standard time from daylight saving time in seconds, if applicable.
    /// </summary>
    public int? DstOffsetToStandardTimeSeconds { get; set; }

    /// <summary>
    /// Gets or sets the start date of daylight saving time, if applicable.
    /// </summary>
    public DateTimeOffset? DstStart { get; set; }

    /// <summary>
    /// Gets or sets the end date of daylight saving time, if applicable.
    /// </summary>
    public DateTimeOffset? DstEnd { get; set; }
}
