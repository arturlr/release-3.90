using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class HomeController : BasePublicController
    {
        public HomeController(IWorkContext workContext) : base(workContext)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Error()
        {
            return Content("An error occurred");
        }
    }
}
