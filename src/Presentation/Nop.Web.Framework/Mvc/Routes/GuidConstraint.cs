using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;


namespace Nop.Web.Framework.Mvc.Routes
{
    public class GuidConstraint : IRouteConstraint
    {
        private readonly bool _allowEmpty;

        public GuidConstraint(bool allowEmpty)
        {
            this._allowEmpty = allowEmpty;
        }

        public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
        {
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
