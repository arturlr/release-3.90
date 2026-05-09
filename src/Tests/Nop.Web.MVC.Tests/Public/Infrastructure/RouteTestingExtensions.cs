//Contributor: MvcContrib.TestHelper
//NOTE: This file has been stubbed out during migration to ASP.NET Core.
//The original route testing infrastructure relied on System.Web.Routing (RouteTable, etc.)
//which does not exist in ASP.NET Core. Route testing should be rewritten using
//Microsoft.AspNetCore.TestHost and WebApplicationFactory-based integration testing.

using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Nop.Web.MVC.Tests.Public.Infrastructure
{
    /// <summary>
    /// Used to simplify testing routes and restful testing routes.
    /// NOTE: This class is a stub. Route testing needs to be rewritten for ASP.NET Core endpoint routing.
    /// </summary>
    public static class RouteTestingExtensions
    {
        /// <summary>
        /// Stub: Returns null. Route testing needs rewrite for ASP.NET Core.
        /// </summary>
        public static RouteData Route(this string url)
        {
            //TODO: Implement using ASP.NET Core routing infrastructure
            return null;
        }

        /// <summary>
        /// Stub: Returns null. Route testing needs rewrite for ASP.NET Core.
        /// </summary>
        public static RouteData ShouldMapTo<TController>(this string relativeUrl, Expression<Func<TController, object>> action) where TController : Controller
        {
            //TODO: Implement using ASP.NET Core routing infrastructure
            return null;
        }
    }
}
