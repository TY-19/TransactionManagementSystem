namespace TMS.Application.Models;

public class TimeZoneDetails
{
    public string TimeZone { get; set; } = string.Empty;
    public int StandardUtcOffsetSeconds { get; set; }
    public bool HasDayLightSaving { get; set; }
    public int? DstOffsetToUtcSeconds { get; set; }
    public int? DstOffsetToStandardTimeSeconds { get; set; }
    public DateTimeOffset? DstStart { get; set; }
    public DateTimeOffset? DstEnd { get; set; }
}
