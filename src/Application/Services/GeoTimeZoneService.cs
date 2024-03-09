using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class GeoTimeZoneService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GeoTimeZoneService> logger
    ) : ITimeZoneService
{
    private const int NumberSecondsInHour = 3600;
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
            return CalculateTimeZoneOffset(deserializedResponse, timestamp) * NumberSecondsInHour;
        }
        else
        {
            logger.LogError("Response of external API is invalid: {@response}", deserializedResponse);
            throw new ArgumentException("Response of external API is invalid");
        }
    }

    private int CalculateTimeZoneOffset(GeoTimeZoneResponse response, long timestamp)
    {
        if (response.Offset == null || !int.TryParse(response.Offset[3..], out int offset))
        {
            logger.LogError("Response of external API does not contain valid offset: {offset}", response.Offset);
            throw new ArgumentException("Response of external API is invalid");
        }

        if (response.DstOffset == null || !int.TryParse(response.DstOffset[3..], out int dstOffset))
            return offset;

        return IsDstActive(timestamp, response.IanaTimezone)
            ? dstOffset
            : offset;
    }

    private static bool IsDstActive(long timestamp, string? IanaTimeZone)
    {
        if (string.IsNullOrEmpty(IanaTimeZone))
            return false;

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(IanaTimeZone, out var timeZone))
            return false;

        DateTimeOffset date = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        return timeZone.IsDaylightSavingTime(date);
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
