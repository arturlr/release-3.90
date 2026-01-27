using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class WidgetController : BasePublicController
    {
        public WidgetController(IWorkContext workContext) : base(workContext)
        {
        }

        // GET: /Widget/WidgetsByZone
        public IActionResult WidgetsByZone(string widgetZone)
        {
            // TODO: Get widgets for zone
            return Content($"Widgets for zone: {widgetZone}");
        }
    }
}
