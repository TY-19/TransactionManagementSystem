using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Services;

public class FreeIpService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<FreeIpService> logger
    ) : IIpService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private string BaseURL => configuration.GetSection("FreeIp")["BaseUrl"] ?? "https://freeipapi.com/api/json";

    /// <inheritdoc cref="IIpService.GetIpAsync(IPAddress?, CancellationToken)"/>
    public async Task<OperationResult<string>> GetIpAsync(IPAddress? ip, CancellationToken cancellationToken)
    {
        string? ipv4 = ip?.MapToIPv4().ToString();
        if (!IsValidIpv4(ipv4, out bool isLocalNetwork))
        {
            return new OperationResult<string>(true) { Payload = ipv4 };
        }
        else if (isLocalNetwork)
        {
            return await GetServerIpAsync(cancellationToken);
        }
        else
        {
            return new OperationResult<string>(false, "IP cannot be determined.");
        }
    }

    private static bool IsValidIpv4(string? ipv4, out bool isLocalNetwork)
    {
        isLocalNetwork = false;
        if (ipv4 == null)
        {
            return false;
        }
        string[] segments = ipv4.Split('.');
        if (segments.Length != 4)
        {
            return false;
        }
        int[] values = new int[4];

        for (int i = 0; i < 4; i++)
        {
            values[i] = int.Parse(segments[i]);
            if (values[i] < 0 || values[i] > 255)
            {
                return false;
            }
        }
        isLocalNetwork = IsLocalNetwork(values);
        return true;
    }

    private static bool IsLocalNetwork(int[] values)
    {
        // IP addresses in the following ranges cannot be resolved by the external API:
        // - 0.0.0.0 - 0.255.255.255, indicating that the host is on the same network (local API call).
        // - 10.0.0.0 – 10.255.255.255
        // - 172.16.0.0 – 172.31.255.255
        // - 192.168.0.0 – 192.168.255.255
        // These ranges are reserved for private internets.
        // For more details, refer to: https://datatracker.ietf.org/doc/html/rfc1918#section-3
        // In this case, since our API receives the request from the local network,
        // it's appropriate to use the server's IP to determine the time zone offset.
        return values[0] == 0
            || values[0] == 10
            || (values[0] == 172 && values[1] >= 16 && values[1] <= 31)
            || (values[0] == 192 && values[1] == 168);
    }

    private async Task<OperationResult<string>> GetServerIpAsync(CancellationToken cancellationToken)
    {
        string urlFull = $"{BaseURL}/";
        logger.LogDebug("Call {url}", urlFull);

        var response = await httpClient.GetAsync(urlFull, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("FreeIp return status code {statusCode}.\r\nRequest url: {url}." +
                "\r\nResponse: {response}", response.StatusCode, urlFull, response);
            return new OperationResult<string>(false, "Request to the external API has failed.");
        }

        try
        {
            using var stream = response.Content.ReadAsStream(cancellationToken);
            var deserializedResponse = JsonSerializer.Deserialize<FreeIpResponse>(stream, jsonSerializerOptions);
            if (deserializedResponse == null)
            {
                return new OperationResult<string>(false, "Cannot process the external API response");
            }
            return new OperationResult<string>(true) { Payload = deserializedResponse.IpAddress };
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred deserializing API response.");
            return new OperationResult<string>(false, "Cannot process the external API response");
        }
    }

    private sealed class FreeIpResponse
    {
        public int IpVersion { get; set; } = 0;
        public string IpAddress { get; set; } = null!;
    }
}
