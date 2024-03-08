using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.Json;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class GoogleMapTimeZoneService(
    HttpClient httpClient,
    IConfiguration configuration
    ) : ITimeZoneService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly string baseUrl = configuration.GetSection("GoogleMapTimeZone")["BaseUrl"]
            ?? "https://maps.googleapis.com";
    private readonly string apiKey = configuration.GetSection("GoogleMapTimeZone")["ApiKey"] ?? "";
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    
    public async Task<int> GetTimeZoneOffsetInSecondsAsync(decimal latitude, decimal longitude, long timestamp)
    {
        string urlFull = $"{baseUrl}/maps/api/timezone/json?location={latitude.ToString(CultureInfo.InvariantCulture)}%2C{longitude.ToString(CultureInfo.InvariantCulture)}&timestamp={timestamp}&key={apiKey}";
        var response = await httpClient.GetAsync(urlFull);

        using var stream = response.Content.ReadAsStream();
        var deserializedResponse = JsonSerializer.Deserialize<GoogleMapApiResponse>(stream, jsonSerializerOptions);

        if(deserializedResponse != null && deserializedResponse.Status == "OK")
        {
            return CalculateTimeZoneOffset(deserializedResponse);
        }
        else
        {
            // Log error
            return -3600;
        }
    }

    private static int CalculateTimeZoneOffset(GoogleMapApiResponse response)
    {
        int raw = response.RawOffset == null ? 0 : response.RawOffset.Value;
        int dst = response.DstOffset == null ? 0 : response.DstOffset.Value;
        return raw + dst;
    }

    private sealed class GoogleMapApiResponse
    {
        public string Status { get; set; } = null!;
        public string? ErrorMessage { get; set; } = null;
        public int? RawOffset { get; set; } = null;
        public int? DstOffset { get; set; } = null;
        public string? TimeZoneId { get; set; } = null;
        public string? TimeZoneName { get; set; } = null;
    }
}
