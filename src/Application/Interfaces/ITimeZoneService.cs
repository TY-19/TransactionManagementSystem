using System.Net;
using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface ITimeZoneService
{
    /// <summary>
    /// Returns an operation result with the status of the operation and the time zone as payload.
    /// </summary>
    /// <param name="ianaName">The IANA name of the time zone to retrieve. Overrides useUserTimeZone.</param>
    /// <param name="useUserTimeZone">
    ///     If true, retrieves the time zone for the provided IPv4 address. 
    ///     If false, specifies that the user's time zone is not needed.
    /// </param>
    /// <param name="ip">The user's IP address.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    ///     An <see cref="OperationResult{T}"/> indicating success if the time zone was found or was not requested; otherwise, failure.
    /// </returns>
    Task<OperationResult<TimeZoneDetails>> GetTimeZoneAsync(string? ianaName,
        bool useUserTimeZone, IPAddress? ip, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an operation result with the status of the operation and the time zone as payload.
    /// </summary>
    /// <param name="ip">The string representation of the IPv4 address of the user.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> indicating success if the time zone was found; otherwise, failure.
    /// </returns>
    Task<OperationResult<TimeZoneDetails>> GetTimeZoneByIpAsync(
        string? ip, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an operation result with the status of the operation and the time zone as payload.
    /// </summary>
    /// <param name="latitude">The latitude of the location to retrieve the time zone for.</param>
    /// <param name="longitude">The longitude of the location to retrieve the time zone for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> indicating success if the time zone was found; otherwise, failure.
    /// </returns>
    Task<OperationResult<TimeZoneDetails>> GetTimeZoneByCoordinatesAsync(
        decimal latitude, decimal longitude, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an operation result with the status of the operation and the time zone as payload.
    /// </summary>
    /// <param name="ianaName">The IANA name of the time zone to retrieve. Overrides useUserTimeZone.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> indicating success if the time zone was found; otherwise, failure.
    /// </returns>
    Task<OperationResult<TimeZoneDetails>> GetTimeZoneByIanaNameAsync(
        string ianaName, CancellationToken cancellationToken);
}
