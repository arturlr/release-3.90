using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Nop.Web.Controllers
{
    public class ChildActionController : Controller
    {
        [HttpGet]
        public IActionResult Invoke(string controller, string action)
        {
            // Prevent recursive child action calls
            if (Request.Headers.ContainsKey("X-Child-Action"))
            {
                var depth = 0;
                if (Request.Headers.TryGetValue("X-Child-Action-Depth", out var depthVal))
                    int.TryParse(depthVal, out depth);
                if (depth > 2)
                    return Content("");
            }

            try
            {
                var controllerType = FindControllerType(controller);
                if (controllerType == null)
                    return Content("");

                var controllerInstance = ActivatorUtilities.CreateInstance(HttpContext.RequestServices, controllerType) as Controller;
                if (controllerInstance == null)
                    return Content("");

                controllerInstance.ControllerContext = new ControllerContext
                {
                    HttpContext = HttpContext,
                    RouteData = RouteData,
                    ActionDescriptor = new ControllerActionDescriptor()
                };

                var method = controllerType.GetMethod(action, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (method == null)
                    return Content("");

                // Build parameters from query string
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var value = Request.Query[param.Name].ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        try
                        {
                            args[i] = Convert.ChangeType(value, param.ParameterType);
                        }
                        catch
                        {
                            args[i] = param.HasDefaultValue ? param.DefaultValue : null;
                        }
                    }
                    else
                    {
                        args[i] = param.HasDefaultValue ? param.DefaultValue : null;
                    }
                }

                var result = method.Invoke(controllerInstance, args);

                if (result is IActionResult actionResult)
                    return actionResult;

                return Content(result?.ToString() ?? "");
            }
            catch
            {
                return Content("");
            }
        }

        private Type FindControllerType(string controllerName)
        {
            var assembly = typeof(HomeController).Assembly;
            var typeName = $"Nop.Web.Controllers.{controllerName}Controller";
            return assembly.GetType(typeName, false, true);
        }
    }
}
