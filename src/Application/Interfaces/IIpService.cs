namespace TMS.Application.Interfaces;

public interface IIpService
{
    Task<int> GetTimeZoneOffsetInMinutesAsync(string? ipv4);
}
