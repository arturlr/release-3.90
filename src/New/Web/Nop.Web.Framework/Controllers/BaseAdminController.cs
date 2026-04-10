using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nop.Web.Framework.Controllers;

/// <summary>
/// Base controller for admin area controllers.
/// Requires AccessAdminPanel permission via ASP.NET Core authorization policy.
/// Sets IsAdmin = true on IWorkContext.
/// </summary>
[Area("Admin")]
[Authorize(Policy = "AccessAdminPanel")]
public abstract class BaseAdminController : BaseController
{
}
