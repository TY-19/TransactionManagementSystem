using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITimeZoneService
{
    /// <summary>
    ///     Returns custom response with the status of the operation and time zone as payload.
    /// </summary>
    /// <param name="IanaName">IANA name of the time zone to get. Overrides useUserTimeZone.</param>
    /// <param name="useUserTimeZone">
    ///     If true gets time zone for the provided ipv4. 
    ///     False specifies that user time zone is not needed.
    /// </param>
    /// <param name="ipv4">String representation of IPv4 of the user</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Succeeded if time zone was found or was not requested, fails otherwise.</returns>
    Task<CustomResponse<TimeZoneDetails>> GetTimeZone(
        string? IanaName, bool useUserTimeZone, string? ipv4, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns custom response with the status of the operation and time zone as payload.
    /// </summary>
    /// <param name="ip">String representation of IPv4 of the user</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Succeeded if time zone was found, fails otherwise.</returns>
    Task<CustomResponse<TimeZoneDetails>> GetTimeZoneByIpAsync(string? ip, CancellationToken cancellationToken);
    /// <summary>
    ///     Returns custom response with the status of the operation and time zone as payload.
    /// </summary>
    /// <param name="latitude">Latitude of the location to get time zone for.</param>
    /// <param name="longitude">Longitude of the location to get time zone for.</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Succeeded if time zone was found, fails otherwise.</returns>
    Task<CustomResponse<TimeZoneDetails>> GetTimeZoneByCoordinatesAsync(
        decimal latitude, decimal longitude, CancellationToken cancellationToken);
    /// <summary>
    ///     Returns custom response with the status of the operation and time zone as payload.
    /// </summary>
    /// <param name="ianaName">IANA name of the time zone to get. Overrides useUserTimeZone</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Succeeded if time zone was found, fails otherwise.</returns>
    Task<CustomResponse<TimeZoneDetails>> GetTimeZoneByIanaNameAsync(
        string ianaName, CancellationToken cancellationToken);
}
