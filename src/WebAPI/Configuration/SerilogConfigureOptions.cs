using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using TMS.Infrastructure.Data;

namespace TMS.WebAPI.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
public class SerilogConfigureOptions(IConfiguration configuration) : IConfigureOptions<LoggerConfiguration>
{
    /// <inheritdoc cref="IConfigureOptions{TOptions}.Configure(TOptions)"/>
    public void Configure(LoggerConfiguration options)
    {
        options
            .MinimumLevel.Debug()
            .ReadFrom.Configuration(configuration)
            .WriteTo.MSSqlServer(configuration.GetConnectionString(
                DbConnectionOptions.DefaultConnectionStringName),
                restrictedToMinimumLevel: LogEventLevel.Warning,
                sinkOptions: new MSSqlServerSinkOptions()
                {
                    TableName = "LogEvents",
                    AutoCreateSqlDatabase = true,
                    AutoCreateSqlTable = true,
                })
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information);
    }
}
