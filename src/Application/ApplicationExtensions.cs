using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TMS.Application.Behaviors;
using TMS.Application.Helpers;
using TMS.Application.Interfaces;
using TMS.Application.Services;

namespace TMS.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddHttpClient();

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITimeZoneService, TimeZoneService>();
        services.AddScoped<IIpService, FreeIpService>();
        services.AddScoped<ITimeZoneHelper, TimeZoneHelper>();
        services.AddScoped<IXlsxHelper, XlsxHelper>();
        services.AddScoped<ICsvHelper, CsvHelper>();
        services.AddSingleton<ITransactionPropertyHelper, TransactionPropertyHelper>();

        return services;
    }
}
