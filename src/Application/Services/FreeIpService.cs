using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class FreeIpService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<FreeIpService> logger
    ) : IIpService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private string BaseURL => configuration.GetSection("FreeIp")["BaseUrl"]
        ?? "https://freeipapi.com/api/json";

    public async Task<int> GetTimeZoneOffsetInMinutesAsync(string? ipv4)
    {
        if (!IsValidIpv4(ipv4, out bool useServerIpInstead))
            throw new ArgumentException("Ip is not in valid format", nameof(ipv4));

        // If request was from local network then use application server ip
        // to determine a user time zone.
        // There are no need to explicitly specify server ip as it will be
        // automatically determined by an external api based on the request.
        if (useServerIpInstead) ipv4 = "";

        string urlFull = $"{BaseURL}/{ipv4}";
        var response = await httpClient.GetAsync(urlFull);

        using var stream = response.Content.ReadAsStream();
        var deserializedResponse = JsonSerializer.Deserialize<FreeIpResponse>(stream, jsonSerializerOptions);
        return CalculateTimeZoneOffset(deserializedResponse);
    }

    private static bool IsValidIpv4(string? ipv4, out bool useServerIpInstead)
    {
        useServerIpInstead = false;
        if (ipv4 == null)
            return false;

        string[] segments = ipv4.Split('.');
        if (segments.Length != 4)
            return false;
        int[] values = new int[4];

        for (int i = 0; i < 4; i++)
        {
            values[i] = int.Parse(segments[i]);
            if (values[i] < 0 || values[i] > 255)
                return false;
        }
        useServerIpInstead = IsLocalNetwork(values);
        return true;
    }

    private static bool IsLocalNetwork(int[] values)
    {
        // IP addresses in the following range cannot be resolved by the external API.
        // 0.0.0.0 - 0.255.255.255 - indicating that the host is on the same network (local API call).
        // The following IP address spaces are reserved for private internets: 
        // 10.0.0.0 – 10.255.255.255
        // 172.16.0.0 – 172.31.255.255
        // 192.168.0.0 – 192.168.255.255
        // See details: https://datatracker.ietf.org/doc/html/rfc1918#section-3

        // Due to the fact that in this case our API receives the request from the local network,
        // it seems appropriate to use the server IP to determine the time zone offset.

        return values[0] == 0
            || values[0] == 10
            || (values[0] == 172 && values[1] >= 16 && values[1] <= 31)
            || (values[0] == 192 && values[1] == 168);
    }

    private int CalculateTimeZoneOffset(FreeIpResponse? response)
    {
        Match match = null!;
        if (response != null && !string.IsNullOrEmpty(response.TimeZone))
        {
            string pattern = @"([+-])(\d{2}):(\d{2})";
            match = Regex.Match(response.TimeZone, pattern);
        }

        if (match == null || !match.Success
            || !int.TryParse(match.Groups[2].Value, out int hours)
            || !int.TryParse(match.Groups[3].Value, out int minutes))
        {
            logger.LogError("Response of external API does not contain valid offset: {response}", response);
            throw new ArgumentException("Response of external API is invalid");
        }

        var totalMinutes = hours * 60 + minutes;
        return match.Groups[1].Value == "-" ? -totalMinutes : totalMinutes;
    }

    private sealed class FreeIpResponse
    {
        public int IpVersion { get; set; } = 0;
        public string IpAddress { get; set; } = string.Empty;
        public decimal Latitude { get; set; } = 0m;
        public decimal Longitude { get; set; } = 0m;
        public string CountryName { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string CityName { get; set; } = string.Empty;
        public string RegionName { get; set; } = string.Empty;
        public string Continent { get; set; } = string.Empty;
        public string ContinentCode { get; set; } = string.Empty;
        public bool IsProxy { get; set; } = false;
    }
}
