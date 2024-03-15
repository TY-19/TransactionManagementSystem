using TMS.Application.Models;

namespace TMS.Application.Interfaces;

public interface IIpService
{
    Task<CustomResponse<string>> GetIpAsync(string? ipv4, CancellationToken cancellationToken);
}
