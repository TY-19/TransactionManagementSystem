using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITimeZoneService
{
    Task<CustomResponse<TimeZoneDetails>> GetTimeZoneByIpAsync(string? ip, CancellationToken cancellationToken);
    Task<CustomResponse<TimeZoneDetails>> GetTimeZoneByCoordinatesAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken);
    Task<CustomResponse<TimeZoneDetails>> GetTimeZoneByIanaNameAsync(string ianaName, CancellationToken cancellationToken);

}
