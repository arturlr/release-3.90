using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.Framework.Mvc.Routes
{
    /// <summary>
    /// Route constraint that requires the value to parse as a <see cref="Guid"/>.
    /// </summary>
    /// <remarks>
    /// Task 6.4: <c>IRouteConstraint</c> exists in ASP.NET Core
    /// (<c>Microsoft.AspNetCore.Routing</c>) but with a different <c>Match</c> signature —
    /// <c>HttpContextBase</c> → <see cref="HttpContext"/>, <c>System.Web.Routing.Route</c> →
    /// <see cref="IRouter"/>, and the parameter is called <c>routeKey</c> rather than
    /// <c>parameterName</c>. Both <c>httpContext</c> and <c>route</c> may be <c>null</c>
    /// during link generation; this implementation never touched either, so nothing is lost.
    /// The matching logic is byte-for-byte the 3.90 logic.
    ///
    /// Instances are still usable in a <c>constraints</c> object passed to
    /// <c>MapControllerRoute</c> / <c>MapLocalizedRoute</c>, because
    /// <c>IRouteConstraint</c> derives from <c>IParameterPolicy</c> and the route-pattern
    /// factory accepts a policy instance as a constraint value — verified against the
    /// net10.0 reference assemblies. The four call sites in
    /// <c>Nop.Web/Infrastructure/RouteProvider.cs</c> (<c>new GuidConstraint(false)</c>)
    /// therefore need no change beyond their own port to <see cref="IEndpointRouteBuilder"/>.
    /// </remarks>
    public class GuidConstraint : IRouteConstraint
    {
        private readonly bool _allowEmpty;

        public GuidConstraint(bool allowEmpty)
        {
            this._allowEmpty = allowEmpty;
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey,
            RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values == null || routeKey == null)
                return false;

            if (values.ContainsKey(routeKey))
            {
                string stringValue = values[routeKey] != null ? values[routeKey].ToString() : null;

                if (!string.IsNullOrEmpty(stringValue))
                {
                    Guid guidValue;

                    return Guid.TryParse(stringValue, out guidValue) &&
                        (_allowEmpty || guidValue != Guid.Empty);
                }
            }

            return false;
        }
    }
}
