namespace TMS.Application.Interfaces;

public interface ITimeZoneService
{
    Task<int> GetTimeZoneOffsetInSecondsAsync(decimal latitude, decimal longitude, long timestamp);
}
