using System.Text;
using Newtonsoft.Json.Converters;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;


namespace Nop.Admin.Controllers
{
    [NopHttpsRequirement(SslRequirement.Yes)]
    [AdminValidateIpAddress]
    [AdminAuthorize]
    [AdminAntiForgery]
    [AdminVendorValidation]
    public abstract partial class BaseAdminController : BaseController
    {
        /// <summary>
        /// Initialize controller
        /// </summary>
        /// <param name="requestContext">Request context</param>
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            //set work context to admin mode
            EngineContext.Current.Resolve<IWorkContext>().IsAdmin = true;

            base.OnActionExecuting(context);
        }

        
        /// <summary>
        /// Access denied view
        /// </summary>
        /// <returns>Access denied view</returns>
        protected virtual ActionResult AccessDeniedView()
        {
            //return new UnauthorizedResult();
            return RedirectToAction("AccessDenied", "Security", new { pageUrl = this.Request.Path.Value + this.Request.QueryString.Value });
        }

        /// <summary>
        /// Access denied json data for kendo grid
        /// </summary>
        /// <returns>Access denied json data</returns>
        protected JsonResult AccessDeniedKendoGridJson()
        {
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            
            return ErrorForKendoGridJson(localizationService.GetResource("Admin.AccessDenied.Description"));
        }

        /// <summary>
        /// Save selected TAB name
        /// </summary>
        /// <param name="tabName">Tab name to save; empty to automatically detect it</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void SaveSelectedTabName(string tabName = "", bool persistForTheNextRequest = true)
        {
            //keep this method synchronized with
            //"GetSelectedTabName" method of \Nop.Web.Framework\HtmlExtensions.cs
            if (string.IsNullOrEmpty(tabName))
            {
                tabName = this.Request.Form["selected-tab-name"];
            }
            
            if (!string.IsNullOrEmpty(tabName))
            {
                const string dataKey = "nop.selected-tab-name";
                if (persistForTheNextRequest)
                {
                    TempData[dataKey] = tabName;
                }
                else
                {
                    ViewData[dataKey] = tabName;
                }
            }
        }


    }
}
