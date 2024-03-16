using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace TMS.WebAPI.Configuration;

/// <inheritdoc cref="IConfigureOptions{TOptions}"/>
public class SwaggerConfigureOptions : IConfigureOptions<SwaggerGenOptions>
{
    /// <inheritdoc cref="IConfigureOptions{TOptions}.Configure(TOptions)"/>
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "Transaction Management System", Version = "v1" });
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    }
}
