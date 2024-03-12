using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class GeoTimeZoneService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<GeoTimeZoneService> logger
    ) : ITimeZoneService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly JsonSerializerOptions jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };
    private string BaseUrl => configuration.GetSection("GeoTimeZone")["BaseUrl"]
        ?? "http://api.geotimezone.com/public/timezone";
    public async Task<int> GetTimeZoneOffsetInMinutesAsync(decimal latitude, decimal longitude, long timestamp)
    {
        string urlFull = $"{BaseUrl}?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}";
        var response = await httpClient.GetAsync(urlFull);

        using var stream = response.Content.ReadAsStream();
        var deserializedResponse = JsonSerializer.Deserialize<GeoTimeZoneResponse>(stream, jsonSerializerOptions);
        return CalculateTimeZoneOffset(deserializedResponse, timestamp);
    }

    private int CalculateTimeZoneOffset(GeoTimeZoneResponse? response, long timestamp)
    {
        int? offset = null;
        if (response != null)
            offset = GetOffsetInMinutes(response.Offset);

        if (response == null || offset == null)
        {
            logger.LogError("Response of external API does not contain valid offset: {response}", response);
            throw new ArgumentException("Response of external API is invalid");
        }

        int? dstOffset = GetOffsetInMinutes(response.DstOffset);
        if (dstOffset == null)
            return offset.Value;

        return IsDstActive(timestamp, response.IanaTimezone)
            ? dstOffset.Value
            : offset.Value;
    }

    private static int? GetOffsetInMinutes(string? toParse)
    {
        if (toParse == null) return null;

        string pattern = @"([+-])(\d{1,2}):?(\d{1,2})?";
        Match match = Regex.Match(toParse, pattern);
        if (!match.Success || !int.TryParse(match.Groups[2].Value, out int hours))
            return null;

        int offset = hours * 60;
        if (int.TryParse(match.Groups[3].Value, out int minutes))
            offset += minutes;

        return match.Groups[1].Value == "-" ? -offset : offset;
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
