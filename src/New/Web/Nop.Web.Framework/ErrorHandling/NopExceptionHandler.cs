using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nop.Core;
using Nop.Services.Logging;
using NopLogLevel = Nop.Core.Domain.Logging.LogLevel;

namespace Nop.Web.Framework.ErrorHandling;

/// <summary>
/// Global exception handler. Logs unhandled exceptions to the nopCommerce DB logger
/// and returns ProblemDetails (API) or redirects to the error page (browser).
/// </summary>
public sealed class NopExceptionHandler(
    IHostEnvironment env,
    ILogger<NopExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception at {Url}", httpContext.Request.Path);

        // Best-effort DB log via INopLogger (may not be registered yet)
        try
        {
            var nopLogger = httpContext.RequestServices.GetService<INopLogger>();
            if (nopLogger is not null)
            {
                var workContext = httpContext.RequestServices.GetService<IWorkContext>();
                var customer = workContext?.CurrentCustomer;
                var url = $"{httpContext.Request.Method} {httpContext.Request.Path}{httpContext.Request.QueryString}";
                nopLogger.InsertLog(NopLogLevel.Error, $"Unhandled: {url} — {exception.Message}", exception.ToString(), customer);
            }
        }
        catch
        {
            // Don't let logging failures mask the original exception
        }

        if (IsApiRequest(httpContext))
        {
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An error occurred processing your request.",
                Detail = env.IsDevelopment() ? exception.ToString() : null
            };
            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        httpContext.Response.Redirect("/error");
        return true;
    }

    private static bool IsApiRequest(HttpContext ctx) =>
        ctx.Request.Headers.Accept.Any(h => h?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true)
        && !ctx.Request.Headers.Accept.Any(h => h?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
}
