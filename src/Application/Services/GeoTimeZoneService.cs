using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class GeoTimeZoneService(
    HttpClient httpClient,
    IConfiguration configuration
    ) : ITimeZoneService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly string baseUrl = configuration.GetSection("GeoTimeZone")["BaseUrl"]
        ?? "http://api.geotimezone.com";
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    public async Task<int> GetTimeZoneOffsetInSecondsAsync(decimal latitude, decimal longitude, long timestamp)
    {
        string urlFull = $"{baseUrl}/public/timezone?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}";
        var response = await httpClient.GetAsync(urlFull);

        using var stream = response.Content.ReadAsStream();
        var deserializedResponse = JsonSerializer.Deserialize<GeoTimeZoneResponse>(stream, jsonSerializerOptions);
        if (deserializedResponse != null)
        {
            return CalculateTimeZoneOffset(deserializedResponse);
        }
        else
        {
            // Log error
            return -3600;
        }
    }
    
    private static int CalculateTimeZoneOffset(GeoTimeZoneResponse response)
    {
        int offset = 0;
        if (response.Offset != null && int.TryParse(response.Offset[3..], out offset))
            offset *= 3600;
        
        int dstOffset = 0;
        if (response.DstOffset != null && int.TryParse(response.DstOffset[3..], out dstOffset))
            dstOffset *= 3600;

        //TimeZoneInfo.FindSystemTimeZoneById()
        //Check if dst is in effect
        //TimeZoneInfo.Local.IsDaylightSavingTime(time)

        return offset;
    }

    private sealed class GeoTimeZoneResponse
    {
        public decimal Longitude { get; set; } = 0m;
        public decimal Latitude { get; set; } = 0m;
        public string? Location { get; set; } = null;
        [JsonPropertyName("country_iso")]
        public string? Country_iso { get; set; } = null;
        [JsonPropertyName("iana_timezone")]
        public string? IanaTimezone { get; set; } = null;
        [JsonPropertyName("TimeZone_abbreviation")]
        public string? TimeZoneAbbreviation { get; set; } = null;
        [JsonPropertyName("Dst_abbreviation")]
        public string? DstAbbreviation { get; set; } = null;
        public string? Offset { get; set; } = null;
        [JsonPropertyName("Dst_offset")]
        public string? DstOffset { get; set; } = null;
        [JsonPropertyName("Current_local_datetime")]
        public string? CurrentLocalDatetime { get; set; } = null;
        [JsonPropertyName("Current_utc_datetime")]
        public string? CurrentUtcDatetime { get; set; } = null;
    }
}
