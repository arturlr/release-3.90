using Microsoft.AspNetCore.Mvc;

namespace Nop.Web.Framework
{
    /// <summary>
    /// URL helper extensions.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3: <c>System.Web.Mvc.UrlHelper</c> → <see cref="IUrlHelper"/>.
    /// <c>Action(string, string, object)</c> is now an extension method on
    /// <see cref="IUrlHelper"/> (<c>Microsoft.AspNetCore.Mvc.UrlHelperExtensions</c>) rather than
    /// an instance method, but the call syntax and the generated URL are unchanged.
    /// </remarks>
    public static class UrlHelperExtensions
    {
        public static string LogOn(this IUrlHelper urlHelper, string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                return urlHelper.Action("Login", "Customer", new { ReturnUrl = returnUrl });
            return urlHelper.Action("Login", "Customer");
        }

        public static string LogOff(this IUrlHelper urlHelper, string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
                return urlHelper.Action("Logout", "Customer", new { ReturnUrl = returnUrl });
            return urlHelper.Action("Logout", "Customer");
        }
    }
}
