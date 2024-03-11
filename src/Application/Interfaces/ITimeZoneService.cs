namespace TMS.Application.Interfaces;

public interface ITimeZoneService
{
    Task<int> GetTimeZoneOffsetInMinutesAsync(decimal latitude, decimal longitude, long timestamp);
}
