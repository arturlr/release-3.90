using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.Values["action"]?.ToString();

            ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                var viewEngine = EngineContext.Current.Resolve<ICompositeViewEngine>();
                var viewResult = viewEngine.FindView(ControllerContext, viewName, false);

                if (!viewResult.Success)
                    return string.Empty;

                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
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
        protected virtual void SuccessNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Success, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        protected virtual void ErrorNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Error, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display error notification
        /// </summary>
        protected virtual void ErrorNotification(Exception exception, bool persistForTheNextRequest = true, bool logException = true)
        {
            if (logException)
                LogException(exception);
            AddNotification(NotifyType.Error, exception.Message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display warning notification
        /// </summary>
        protected virtual void WarningNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Warning, message, persistForTheNextRequest);
        }

        /// <summary>
        /// Display notification
        /// </summary>
        protected virtual void AddNotification(NotifyType type, string message, bool persistForTheNextRequest)
        {
            string dataKey = string.Format("nop.notifications.{0}", type);
            if (persistForTheNextRequest)
            {
                if (TempData[dataKey] == null)
                    TempData[dataKey] = new List<string>();
                ((List<string>)TempData[dataKey]).Add(message);
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
        protected virtual void DisplayEditLink(string editPageUrl)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddEditPageUrl(editPageUrl);
        }

        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
        protected virtual void AddLocales<TLocalizedModelLocal>(ILanguageService languageService, IList<TLocalizedModelLocal> locales) where TLocalizedModelLocal : ILocalizedModelLocal
        {
            AddLocales(languageService, locales, null);
        }

        /// <summary>
        /// Add locales for localizable entities
        /// </summary>
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
