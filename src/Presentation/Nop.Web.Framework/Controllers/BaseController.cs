using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Stores;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.UI;

namespace Nop.Web.Framework.Controllers
{
    /// <summary>
    /// Base controller
    /// </summary>
    /// <remarks>
    /// Task 6.2: <c>System.Web.Mvc.Controller</c> -&gt;
    /// <see cref="Microsoft.AspNetCore.Mvc.Controller"/> (Requirement 4.3).
    /// <list type="bullet">
    /// <item><c>ViewEngines.Engines.FindPartialView(ControllerContext, viewName)</c> -&gt;
    /// <see cref="ICompositeViewEngine.FindView(ActionContext, string, bool)"/> resolved from
    /// <c>HttpContext.RequestServices</c>. The static engine collection does not exist in
    /// ASP.NET Core.</item>
    /// <item><c>IView.Render(ViewContext, TextWriter)</c> is asynchronous in ASP.NET Core
    /// (<c>RenderAsync</c>). <see cref="RenderPartialViewToString(string, object)"/> keeps its
    /// synchronous signature - it is called from ~dozens of controller actions in Nop.Web and
    /// Nop.Admin - so the task is blocked with
    /// <c>GetAwaiter().GetResult()</c>. ASP.NET Core installs no
    /// <c>SynchronizationContext</c>, so this cannot deadlock; it occupies the request thread.
    /// Same trade-off already accepted for <c>FormsAuthenticationService</c> in task 4.2.</item>
    /// <item><c>RouteData.GetRequiredString("action")</c> -&gt;
    /// <c>ControllerContext.ActionDescriptor.ActionName</c>. ASP.NET Core's
    /// <c>RouteValueDictionary</c> has no <c>GetRequiredString</c>.</item>
    /// <item><c>new ViewContext(ControllerContext, view, ViewData, TempData, writer)</c> -&gt; the
    /// ASP.NET Core overload additionally takes an <see cref="HtmlHelperOptions"/>, taken from
    /// the configured <c>MvcViewOptions</c> so the rendered partial obeys the same HTML options
    /// as a normally rendered view.</item>
    /// <item>Notifications: <c>TempData</c> in ASP.NET Core round-trips through a serializer
    /// rather than session-stored CLR objects, so the value is read back through
    /// <c>IList&lt;string&gt;</c> and re-assigned rather than mutated in place. See the task 6.2
    /// report for the deferral this implies for consumers that cast to
    /// <c>List&lt;string&gt;</c>.</item>
    /// </list>
    /// </remarks>
    [StoreIpAddress]
    [CustomerLastActivity]
    [StoreLastVisitedPage]
    [ValidatePassword]
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// Render partial view to string
        /// </summary>
        /// <returns>Result</returns>
        public virtual string RenderPartialViewToString()
        {
            return RenderPartialViewToString(null, null);
        }
        /// <summary>
        /// Render partial view to string
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <returns>Result</returns>
        public virtual string RenderPartialViewToString(string viewName)
        {
            return RenderPartialViewToString(viewName, null);
        }
        /// <summary>
        /// Render partial view to string
        /// </summary>
        /// <param name="model">Model</param>
        /// <returns>Result</returns>
        public virtual string RenderPartialViewToString(object model)
        {
            return RenderPartialViewToString(null, model);
        }
        /// <summary>
        /// Render partial view to string
        /// </summary>
        /// <param name="viewName">View name</param>
        /// <param name="model">Model</param>
        /// <returns>Result</returns>
        public virtual string RenderPartialViewToString(string viewName, object model)
        {
            //Original source code: http://craftycodeblog.com/2010/05/15/asp-net-mvc-render-partial-view-to-string/
            if (string.IsNullOrEmpty(viewName))
                viewName = this.ControllerContext.ActionDescriptor.ActionName;

            this.ViewData.Model = model;

            var serviceProvider = this.HttpContext.RequestServices;
            var viewEngine = serviceProvider.GetRequiredService<ICompositeViewEngine>();
            var htmlHelperOptions = serviceProvider
                .GetRequiredService<Microsoft.Extensions.Options.IOptions<MvcViewOptions>>()
                .Value.HtmlHelperOptions;

            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = viewEngine.FindView(this.ControllerContext, viewName, false);
                if (viewResult == null || !viewResult.Success)
                    throw new NopException(string.Format("Partial view '{0}' was not found", viewName));

                var viewContext = new ViewContext(this.ControllerContext, viewResult.View,
                    this.ViewData, this.TempData, sw, htmlHelperOptions);
                viewResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();

                return sw.GetStringBuilder().ToString();
            }
        }


        /// <summary>
        /// Get active store scope (for multi-store configuration mode)
        /// </summary>
        /// <param name="storeService">Store service</param>
        /// <param name="workContext">Work context</param>
        /// <returns>Store ID; 0 if we are in a shared mode</returns>
        public virtual int GetActiveStoreScopeConfiguration(IStoreService storeService, IWorkContext workContext)
        {
            //ensure that we have 2 (or more) stores
            if (storeService.GetAllStores().Count < 2)
                return 0;


            var storeId = workContext.CurrentCustomer.GetAttribute<int>(SystemCustomerAttributeNames.AdminAreaStoreScopeConfiguration);
            var store = storeService.GetStoreById(storeId);
            return store != null ? store.Id : 0;
        }


        /// <summary>
        /// Log exception
        /// </summary>
        /// <param name="exc">Exception</param>
        protected void LogException(Exception exc)
        {
            var workContext = EngineContext.Current.Resolve<IWorkContext>();
            var logger = EngineContext.Current.Resolve<ILogger>();

            var customer = workContext.CurrentCustomer;
            logger.Error(exc.Message, exc, customer);
        }
        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void SuccessNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Success, message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void ErrorNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Error, message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        protected virtual void ErrorNotification(Exception exception, bool persistForTheNextRequest = true, bool logException = true)
        {
            if (logException)
                LogException(exception);
            AddNotification(NotifyType.Error, exception.Message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display warning notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void WarningNotification(string message, bool persistForTheNextRequest = true) {
            AddNotification(NotifyType.Warning, message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display notification
        /// </summary>
        /// <param name="type">Notification type</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void AddNotification(NotifyType type, string message, bool persistForTheNextRequest)
        {
            string dataKey = string.Format("nop.notifications.{0}", type);
            if (persistForTheNextRequest)
            {
                //ASP.NET Core TempData is serialized, so re-assign instead of mutating in place
                var messages = TempData[dataKey] as IList<string> ?? new List<string>();
                messages.Add(message);
                TempData[dataKey] = messages;
            }
            else
            {
                if (ViewData[dataKey] == null)
                    ViewData[dataKey] = new List<string>();
                ((List<string>)ViewData[dataKey]).Add(message);
            }
        }

        /// <summary>
        /// Error's json data for kendo grid
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Error's json data</returns>
        protected JsonResult ErrorForKendoGridJson(string errorMessage)
        {
            var gridModel = new DataSourceResult
            {
                Errors = errorMessage
            };

            return Json(gridModel);
        }

        /// <summary>
        /// Display "Edit" (manage) link (in public store)
        /// </summary>
        /// <param name="editPageUrl">Edit page URL</param>
        protected virtual void DisplayEditLink(string editPageUrl)
        {
            //We cannot use ViewData because it works only for the current controller (and we pass and then render "Edit" link data in distinct controllers)
            //that's why we use IPageHeadBuilder
            //ViewData["nop.editpage.link"] = editPageUrl;
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddEditPageUrl(editPageUrl);
        }



        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        /// <typeparam name="TLocalizedModelLocal">Localizable model</typeparam>
        /// <param name="languageService">Language service</param>
        /// <param name="locales">Locales</param>
        protected virtual void AddLocales<TLocalizedModelLocal>(ILanguageService languageService, IList<TLocalizedModelLocal> locales) where TLocalizedModelLocal : ILocalizedModelLocal
        {
            AddLocales(languageService, locales, null);
        }
        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        /// <typeparam name="TLocalizedModelLocal">Localizable model</typeparam>
        /// <param name="languageService">Language service</param>
        /// <param name="locales">Locales</param>
        /// <param name="configure">Configure action</param>
        protected virtual void AddLocales<TLocalizedModelLocal>(ILanguageService languageService, IList<TLocalizedModelLocal> locales, Action<TLocalizedModelLocal, int> configure) where TLocalizedModelLocal : ILocalizedModelLocal
        {
            foreach (var language in languageService.GetAllLanguages(true))
            {
                var locale = Activator.CreateInstance<TLocalizedModelLocal>();
                locale.LanguageId = language.Id;
                if (configure != null)
                {
                    configure.Invoke(locale, locale.LanguageId);
                }
                locales.Add(locale);
            }
        }

    }
}
