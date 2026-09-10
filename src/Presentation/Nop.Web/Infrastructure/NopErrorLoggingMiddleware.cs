using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;

namespace Nop.Web.Infrastructure
{
    /// <summary>
    /// The logging half of 3.90's <c>Global.asax.cs</c> <c>Application_Error</c> (task 7.2,
    /// Requirement 4.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Application_Error</c> did three things. <c>Program.cs</c> splits them across the three
    /// ASP.NET Core mechanisms that own them:
    /// <list type="number">
    /// <item><b>presentation of unhandled exceptions</b> —
    /// <c>UseDeveloperExceptionPage()</c> / <c>UseExceptionHandler(...)</c>, replacing
    /// <c>&lt;customErrors defaultRedirect="errorpage.htm" mode="RemoteOnly"&gt;</c>;</item>
    /// <item><b>re-execution of <c>CommonController.PageNotFound</c> for 404s</b> —
    /// <c>UseStatusCodePagesWithReExecute("/page-not-found")</c>, replacing the
    /// <c>Response.Clear()</c> / <c>Server.ClearError()</c> / <c>errorController.Execute(...)</c>
    /// block;</item>
    /// <item><b>logging</b> — this middleware.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Why logging needs its own middleware rather than riding on
    /// <c>UseExceptionHandler</c>.</b> 3.90's <c>LogException</c> had a 404 rule:
    /// <i>"ignore 404 HTTP errors ... unless <c>CommonSettings.Log404Errors</c>"</i>. In
    /// System.Web a 404 arrived as an <c>HttpException</c>, so one <c>catch</c> covered both
    /// cases. In ASP.NET Core a 404 is a <b>status code, not an exception</b> — nothing throws —
    /// so an exception-only handler would make <c>Log404Errors</c> permanently dead. This
    /// middleware therefore observes both paths: the <c>catch</c> for real exceptions, and the
    /// response status code after <c>_next</c> for 404s.
    /// </para>
    /// <para>
    /// Everything else is 3.90 verbatim: nothing is logged before the database is installed, the
    /// customer is taken from <c>IWorkContext.CurrentCustomer</c>, and a failure <i>inside</i>
    /// logging is swallowed ("don't throw new exception if occurs") because the database is the
    /// log sink and it is usually the thing that just failed. The exception itself is always
    /// rethrown — this middleware observes, it does not handle.
    /// </para>
    /// </remarks>
    public class NopErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public NopErrorLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                LogException(context, exception);
                throw;
            }

            //3.90 processed 404s in Application_Error because System.Web raised them as an
            //HttpException. Here they are just a status code, so they are inspected explicitly.
            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                Log404(context);
        }

        /// <summary>
        /// 3.90's <c>MvcApplication.LogException</c>, minus the <c>HttpException</c>/404 branch
        /// which <see cref="Log404"/> now owns.
        /// </summary>
        protected virtual void LogException(HttpContext context, Exception exception)
        {
            if (exception == null)
                return;

            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            try
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                logger.Error(exception.Message, exception, workContext.CurrentCustomer);
            }
            catch (Exception)
            {
                //don't throw new exception if occurs
            }
        }

        /// <summary>
        /// The <c>CommonSettings.Log404Errors</c> rule from 3.90's <c>LogException</c>, applied to
        /// a 404 response instead of to an <c>HttpException</c>.
        /// </summary>
        protected virtual void Log404(HttpContext context)
        {
            if (!DataSettingsHelper.DatabaseIsInstalled())
                return;

            try
            {
                if (!EngineContext.Current.Resolve<CommonSettings>().Log404Errors)
                    return;

                //3.90's Application_Error skipped static resources before touching the 404 path
                var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                if (webHelper.IsStaticResource(context.Request))
                    return;

                var logger = EngineContext.Current.Resolve<ILogger>();
                var workContext = EngineContext.Current.Resolve<IWorkContext>();
                logger.Error(string.Format("Error 404. The requested page ({0}) was not found.",
                        webHelper.GetThisPageUrl(false)),
                    null, workContext.CurrentCustomer);
            }
            catch (Exception)
            {
                //don't throw new exception if occurs
            }
        }
    }

    /// <summary>
    /// Registration helper for <see cref="NopErrorLoggingMiddleware"/>.
    /// </summary>
    public static class NopErrorLoggingMiddlewareExtensions
    {
        /// <summary>
        /// Logs unhandled exceptions and (subject to <c>CommonSettings.Log404Errors</c>) 404
        /// responses through nopCommerce's own <see cref="ILogger"/>. Register immediately after
        /// the exception handler / status-code pages so it observes everything below it.
        /// </summary>
        public static IApplicationBuilder UseNopErrorLogging(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException("app");

            return app.UseMiddleware<NopErrorLoggingMiddleware>();
        }
    }
}
