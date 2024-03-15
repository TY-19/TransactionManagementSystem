using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace TMS.WebAPI.Middleware;

/// <summary>
///     Provides behavior to handle exceptions.
/// </summary>
public class ExceptionHandler(IHostEnvironment env, ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc cref="IExceptionHandler.TryHandleAsync(HttpContext, Exception, CancellationToken)"/>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        LogException(exception);
        var serialized = GetSerializedResponse(httpContext, exception);
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(serialized, cancellationToken);

        return true;
    }

    private void LogException(Exception exception)
    {
        logger.LogError("An unknown exception has happened.\r\n{message}.\r\nParameters: {parameters}"
            + "\r\nInner exception: {innerExc}.\r\nStack trace: {stackTrace}",
            exception.Message, exception.InnerException, exception.Data, exception.StackTrace);
    }

    private string GetSerializedResponse(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Status = context.Response.StatusCode,
        };
        problemDetails.Title = ReasonPhrases.GetReasonPhrase(problemDetails.Status ?? 0);
        problemDetails.Detail = exception.Message;

        if (env.IsDevelopment())
            problemDetails.Extensions["data"] = exception.Data;

        try
        {
            return JsonSerializer.Serialize(problemDetails);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception has occurred while serializing error to JSON");
            return string.Empty;
        }
    }
}
