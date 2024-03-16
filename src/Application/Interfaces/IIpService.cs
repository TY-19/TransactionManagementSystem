using System.Net;
using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface IIpService
{
    /// <summary>
    ///     Get string representation of an ip address.
    ///     If request originate from local network then return server ip.
    /// </summary>
    /// <param name="ip">IPAddress to transform</param>
    /// <param name="cancellationToken">A cancellation token that is used to receive notice of cancellation.</param>
    /// <returns>Returns an string representation of IPv4</returns>
    Task<CustomResponse<string>> GetIpAsync(IPAddress? ip, CancellationToken cancellationToken);
}
