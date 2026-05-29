using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Infrastructure;
using Nop.Services.Localization;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Security;

namespace Nop.Admin.Controllers
{
    [NopHttpsRequirement(SslRequirement.Yes)]
    [AdminValidateIpAddress]
    [AdminAuthorize]
    [AdminAntiForgery]
    [AdminVendorValidation]
    public abstract partial class BaseAdminController : BaseController
    {
        protected virtual IActionResult AccessDeniedView()
        {
            return RedirectToAction("AccessDenied", "Security", new { pageUrl = Request.Path.ToString() });
        }

        protected JsonResult AccessDeniedKendoGridJson()
        {
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();
            return ErrorForKendoGridJson(localizationService.GetResource("Admin.AccessDenied.Description"));
        }

        protected virtual void SaveSelectedTabName(string tabName = "", bool persistForTheNextRequest = true)
        {
            if (string.IsNullOrEmpty(tabName))
            {
                tabName = Request.Form["selected-tab-name"].ToString();
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
