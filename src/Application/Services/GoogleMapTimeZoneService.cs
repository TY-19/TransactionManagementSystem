using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class GoogleMapTimeZoneService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GoogleMapTimeZoneService> logger
    ) : ITimeZoneService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly string apiKey = configuration.GetSection("GoogleMapTimeZone")["ApiKey"] ?? "";
    private string BaseUrl => configuration.GetSection("GoogleMapTimeZone")["BaseUrl"]
        ?? "https://maps.googleapis.com/maps/api/timezone/json";
    public async Task<int> GetTimeZoneOffsetInMinutesAsync(decimal latitude, decimal longitude, long timestamp)
    {
        string urlFull = $"{BaseUrl}?location={latitude.ToString(CultureInfo.InvariantCulture)}%2C{longitude.ToString(CultureInfo.InvariantCulture)}&timestamp={timestamp}&key={apiKey}";
        var response = await httpClient.GetAsync(urlFull);

        using var stream = response.Content.ReadAsStream();
        var deserializedResponse = JsonSerializer.Deserialize<GoogleMapApiResponse>(stream, jsonSerializerOptions);

        if (deserializedResponse != null && deserializedResponse.Status == "OK")
        {
            return CalculateTimeZoneOffset(deserializedResponse);
        }
        else
        {
            logger.LogError("Response of external API is invalid: {@response}", deserializedResponse);
            throw new ArgumentException("Response of external API is invalid");
        }
    }

    private static int CalculateTimeZoneOffset(GoogleMapApiResponse response)
    {
        int raw = response.RawOffset == null ? 0 : response.RawOffset.Value / 60;
        int dst = response.DstOffset == null ? 0 : response.DstOffset.Value / 60;
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
