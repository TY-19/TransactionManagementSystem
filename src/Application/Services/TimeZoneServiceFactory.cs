using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class TimeZoneServiceFactory : ITimeZoneServiceFactory
{
    private readonly ITimeZoneService timeZoneService;
    public TimeZoneServiceFactory(
        IConfiguration configuration,
        IHttpClientFactory factory,
        ILogger<GoogleMapTimeZoneService> gmLogger,
        ILogger<GeoTimeZoneService> geoLogger
        )
    {
        if (!string.IsNullOrWhiteSpace(configuration["GoogleMapsApiKey"]))
        {
            timeZoneService = new GoogleMapTimeZoneService(factory.CreateClient(), configuration, gmLogger);
        }
        else
        {
            timeZoneService = new GeoTimeZoneService(factory.CreateClient(), configuration, geoLogger);
        }
    }

    public ITimeZoneService GetTimeZoneService() => timeZoneService;
}
