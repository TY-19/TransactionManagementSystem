using System.Net;
using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface IIpService
{
    /// <summary>
    /// Get the string representation of an IP address.
    /// If the request originates from the local network, the server IP is returned.
    /// </summary>
    /// <param name="ip">The IPAddress to transform.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The string representation of the IPv4 address.</returns>
    Task<OperationResult<string>> GetIpAsync(IPAddress? ip, CancellationToken cancellationToken);
}
