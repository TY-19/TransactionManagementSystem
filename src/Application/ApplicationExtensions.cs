using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
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
        });

        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<ITimeZoneServiceFactory, TimeZoneServiceFactory>();

        return services;
    }
}
