using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nop.Web.Controllers;

/// <summary>
/// Serves custom error pages for 404 and 500 status codes.
/// </summary>
[AllowAnonymous]
public class CommonController : Controller
{
    [Route("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("~/Views/Common/Error.cshtml");

    [Route("/page-not-found")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult PageNotFound() => View("~/Views/Common/PageNotFound.cshtml");
}
