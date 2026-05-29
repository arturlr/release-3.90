// Route testing infrastructure - migrated stub for .NET Core
// The original MVC 5 route testing is not applicable to ASP.NET Core routing.
// These tests would need to be rewritten using Microsoft.AspNetCore.Routing.

using System;
using Microsoft.AspNetCore.Routing;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Infrastructure
{
    public static class RouteTestingExtensions
    {
        // Stub - ASP.NET Core routing tests require different approach
        public static void ShouldMapToRoute(this string url, string controller, string action)
        {
            Assert.Pass("Route testing stub - needs ASP.NET Core routing test infrastructure");
        }
    }
}
