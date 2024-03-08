using Microsoft.Extensions.Configuration;
using TMS.Application.Interfaces;

namespace TMS.Application.Services;

public class TimeZoneServiceFactory : ITimeZoneServiceFactory
{
    private readonly ITimeZoneService timeZoneService;
    public TimeZoneServiceFactory(IConfiguration configuration, IHttpClientFactory factory)
    {
        if (!string.IsNullOrWhiteSpace(configuration["GoogleMapsApiKey"]))
        {
            timeZoneService = new GoogleMapTimeZoneService(factory.CreateClient(), configuration);
        }
        else
        {
            timeZoneService = new GeoTimeZoneService(factory.CreateClient(), configuration);
        }
    }

    public ITimeZoneService GetTimeZoneService() => timeZoneService;
}
