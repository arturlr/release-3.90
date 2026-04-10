using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Areas.Admin.Models.Home;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Areas.Admin.Controllers;

public class HomeController(IWorkContext workContext) : BaseAdminController
{
    public IActionResult Index()
    {
        var model = new DashboardModel
        {
            IsLoggedInAsVendor = workContext.CurrentVendor is not null
        };
        return View(model);
    }
}
