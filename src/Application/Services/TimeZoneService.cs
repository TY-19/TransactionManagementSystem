﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text.Json;
using TMS.Application.Interfaces;
using TMS.Application.Models;

namespace TMS.Application.Services;

public class TimeZoneService(
    HttpClient httpClient,
    IIpService ipService,
    IConfiguration configuration,
    ILogger<TimeZoneService> logger
    ) : ITimeZoneService
{
    private readonly HttpClient httpClient = httpClient;
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private string BaseUrl => configuration.GetSection("TimeApi")["BaseUrl"] ?? "https://timeapi.io/api/TimeZone";

    /// <inheritdoc cref="ITimeZoneService.GetTimeZone(string?, bool, string?, CancellationToken)"/>
    public async Task<OperationResult<TimeZoneDetails>> GetTimeZoneAsync(string? ianaName, bool useUserTimeZone,
        IPAddress? ip, CancellationToken cancellationToken)
    {
        if (ianaName != null)
        {
            return await GetTimeZoneByIanaNameAsync(ianaName, cancellationToken);
        }
        if (useUserTimeZone)
        {
            OperationResult<string> ipsResponse = await ipService.GetIpAsync(ip, cancellationToken);
            if (!ipsResponse.Succeeded)
            {
                return new OperationResult<TimeZoneDetails>(false, ipsResponse.Message ?? "Ip service error");
            }
            return await GetTimeZoneByIpAsync(ipsResponse.Payload, cancellationToken);
        }

        return new OperationResult<TimeZoneDetails>(true);
    }

    /// <inheritdoc cref="ITimeZoneService.GetTimeZoneByIpAsync(string?, CancellationToken)"/>
    public async Task<OperationResult<TimeZoneDetails>> GetTimeZoneByIpAsync(string? ip, CancellationToken cancellationToken)
    {
        string urlFull = $"{BaseUrl}/ip?ipAddress={ip}";
        return await CallExternalApiAsync(urlFull, cancellationToken);
    }

    /// <inheritdoc cref="ITimeZoneService.GetTimeZoneByCoordinatesAsync(decimal, decimal, CancellationToken)(string?, CancellationToken)"/>
    public async Task<OperationResult<TimeZoneDetails>> GetTimeZoneByCoordinatesAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken)
    {
        string urlFull = $"{BaseUrl}/coordinate?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}";
        return await CallExternalApiAsync(urlFull, cancellationToken);
    }

    /// <inheritdoc cref="ITimeZoneService.GetTimeZoneByIanaNameAsync(string, CancellationToken)"/>
    public async Task<OperationResult<TimeZoneDetails>> GetTimeZoneByIanaNameAsync(string ianaName, CancellationToken cancellationToken)
    {
        string urlFull = $"{BaseUrl}/zone?timeZone={ianaName.Replace(" ", "")}";
        return await CallExternalApiAsync(urlFull, cancellationToken);
    }

    private async Task<OperationResult<TimeZoneDetails>> CallExternalApiAsync(string url, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response;
        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new OperationResult<TimeZoneDetails>(false, "Time zone was not found.");
            }
            response.EnsureSuccessStatusCode();
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has happened when calling the external API. Url: {url}", url);
            return new OperationResult<TimeZoneDetails>(false);
        }

        try
        {
            using var stream = response.Content.ReadAsStream(cancellationToken);
            var deserializedResponse = JsonSerializer.Deserialize<TimeZoneApiResponse>(stream, jsonSerializerOptions);
            if (deserializedResponse == null)
            {
                return new OperationResult<TimeZoneDetails>(false, "Cannot process the external API response.");
            }
            return new OperationResult<TimeZoneDetails>(true, ToTimeZoneDetails(deserializedResponse));
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error has occurred deserializing API response. Url: {url}", url);
            return new OperationResult<TimeZoneDetails>(false, "Cannot process the external API response.");
        }
    }

    private static TimeZoneDetails ToTimeZoneDetails(TimeZoneApiResponse response)
    {
        return new TimeZoneDetails()
        {
            TimeZoneName = response.TimeZone,
            StandardUtcOffsetSeconds = response.StandardUtcOffset.Seconds,
            HasDayLightSaving = response.HasDayLightSaving,
            DstOffsetToUtcSeconds = response.DstInterval?.DstOffsetToUtc.Seconds,
            DstOffsetToStandardTimeSeconds = response.DstInterval?.DstOffsetToStandardTime.Seconds,
            DstStart = response.DstInterval?.DstStart,
            DstEnd = response.DstInterval?.DstEnd
        };
    }

    private sealed class TimeZoneApiResponse
    {
        public string TimeZone { get; set; } = null!;
        public DateTimeOffset CurrentLocalTime { get; set; } = default!;
        public UtcOffset CurrentUtcOffset { get; set; } = null!;
        public UtcOffset StandardUtcOffset { get; set; } = null!;
        public bool HasDayLightSaving { get; set; } = false;
        public bool IsDayLightSavingActive { get; set; } = false;
        public DstInterval? DstInterval { get; set; } = null;
    }

    private sealed class UtcOffset
    {
        public int Seconds { get; set; } = default;
        public int Milliseconds { get; set; } = default;
        public long Ticks { get; set; } = default;
        public long Nanoseconds { get; set; } = default;
    }

    private sealed class DstInterval
    {
        public string DstName { get; set; } = default!;
        public UtcOffset DstOffsetToUtc { get; set; } = default!;
        public UtcOffset DstOffsetToStandardTime { get; set; } = default!;
        public DateTimeOffset DstStart { get; set; } = default;
        public DateTimeOffset DstEnd { get; set; } = default;
    }
}
