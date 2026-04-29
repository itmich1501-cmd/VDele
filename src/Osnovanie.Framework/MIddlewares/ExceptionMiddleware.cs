using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Osnovanie.Shared;

namespace Osnovanie.Framework.MIddlewares;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred");

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var error = Error.Failure(
                "general.unexpected",
                "Произошла непредвиденная ошибка");

            await httpContext.Response.WriteAsJsonAsync(
                Envelope.Error(error.ToErrors()));
        }
    }
}