using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers;

public class HomeController : BasePublicController
{
    public IActionResult Index() => View();
}
