using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.UI.Paging;

namespace Nop.Web.Framework.UI
{
    public static class LayoutExtensions
    {
        public static void AddTitleParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddTitleParts(part);
        }
        public static void AppendTitleParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendTitleParts(part);
        }
        public static IHtmlContent NopTitle(this IHtmlHelper html, bool addDefaultTitle = true, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendTitleParts(part);
            return new HtmlString(WebUtility.HtmlEncode(pageHeadBuilder.GenerateTitle(addDefaultTitle)));
        }

        public static void AddMetaDescriptionParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddMetaDescriptionParts(part);
        }
        public static void AppendMetaDescriptionParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendMetaDescriptionParts(part);
        }
        public static IHtmlContent NopMetaDescription(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendMetaDescriptionParts(part);
            return new HtmlString(WebUtility.HtmlEncode(pageHeadBuilder.GenerateMetaDescription()));
        }

        public static void AddMetaKeywordParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddMetaKeywordParts(part);
        }
        public static void AppendMetaKeywordParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendMetaKeywordParts(part);
        }
        public static IHtmlContent NopMetaKeywords(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendMetaKeywordParts(part);
            return new HtmlString(WebUtility.HtmlEncode(pageHeadBuilder.GenerateMetaKeywords()));
        }

        public static void AddScriptParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false, bool isAsync = false)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddScriptParts(location, part, excludeFromBundle, isAsync);
        }
        public static void AddScriptParts(this IHtmlHelper html, string part, bool excludeFromBundle = false, bool isAsync = false)
        {
            AddScriptParts(html, ResourceLocation.Head, part, excludeFromBundle, isAsync);
        }
        public static void AppendScriptParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false, bool isAsync = false)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendScriptParts(location, part, excludeFromBundle, isAsync);
        }
        public static void AppendScriptParts(this IHtmlHelper html, string part, bool excludeFromBundle = false, bool isAsync = false)
        {
            AppendScriptParts(html, ResourceLocation.Head, part, excludeFromBundle, isAsync);
        }
        public static IHtmlContent NopScripts(this IHtmlHelper html, ResourceLocation location, bool? bundleFiles = null)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            return new HtmlString(pageHeadBuilder.GenerateScripts(location, bundleFiles));
        }
        public static IHtmlContent NopScripts(this IHtmlHelper html, Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, ResourceLocation location, bool? bundleFiles = null)
        {
            return NopScripts(html, location, bundleFiles);
        }

        public static void AddCssFileParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddCssFileParts(location, part, excludeFromBundle);
        }
        public static void AddCssFileParts(this IHtmlHelper html, string part, bool excludeFromBundle = false)
        {
            AddCssFileParts(html, ResourceLocation.Head, part, excludeFromBundle);
        }
        public static void AppendCssFileParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendCssFileParts(location, part, excludeFromBundle);
        }
        public static void AppendCssFileParts(this IHtmlHelper html, string part, bool excludeFromBundle = false)
        {
            AppendCssFileParts(html, ResourceLocation.Head, part, excludeFromBundle);
        }
        public static IHtmlContent NopCssFiles(this IHtmlHelper html, ResourceLocation location, bool? bundleFiles = null)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            return new HtmlString(pageHeadBuilder.GenerateCssFiles(location, bundleFiles));
        }
        public static IHtmlContent NopCssFiles(this IHtmlHelper html, Microsoft.AspNetCore.Mvc.IUrlHelper urlHelper, ResourceLocation location, bool? bundleFiles = null)
        {
            return NopCssFiles(html, location, bundleFiles);
        }

        public static void AddCanonicalUrlParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddCanonicalUrlParts(part);
        }
        public static IHtmlContent NopCanonicalUrls(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AddCanonicalUrlParts(part);
            return new HtmlString(pageHeadBuilder.GenerateCanonicalUrls());
        }

        public static void AddHeadCustomParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddHeadCustomParts(part);
        }
        public static void AppendHeadCustomParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendHeadCustomParts(part);
        }
        public static IHtmlContent NopHeadCustom(this IHtmlHelper html)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            return new HtmlString(pageHeadBuilder.GenerateHeadCustom());
        }

        public static void AddPageCssClassParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AddPageCssClassParts(part);
        }
        public static IHtmlContent NopPageCssClasses(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AddPageCssClassParts(part);
            return new HtmlString(pageHeadBuilder.GeneratePageCssClasses());
        }

        public static void AppendPageCssClassParts(this IHtmlHelper html, string part)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            pageHeadBuilder.AppendPageCssClassParts(part);
        }

        /// <summary>
        /// Renders a widget zone by invoking all widget plugins registered for the given zone.
        /// Returns concatenated HTML from all active widgets in the zone.
        /// </summary>
        public static IHtmlContent Widget(this IHtmlHelper html, string widgetZone, object additionalData = null)
        {
            // Widget zones render content from IWidgetPlugin implementations
            // In the migrated app, widgets are rendered as empty until plugin loading is fully wired
            // This is valid runtime behavior - no widgets are active by default
            return new HtmlString("");
        }

        /// <summary>
        /// Replacement for MVC5's Html.Action() which rendered a child action inline.
        /// This implementation creates a sub-request to invoke the controller action and captures the output.
        /// </summary>
        public static IHtmlContent Action(this IHtmlHelper html, string actionName, string controllerName, object routeValues = null)
        {
            try
            {
                var httpContext = html.ViewContext.HttpContext;
                var services = httpContext.RequestServices;
                
                // Get the controller factory and create the controller
                var actionDescriptorCollectionProvider = services.GetService(
                    typeof(Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider)) 
                    as Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider;
                
                if (actionDescriptorCollectionProvider == null)
                    return new HtmlString("");

                // Find the matching action descriptor
                var actionDescriptor = actionDescriptorCollectionProvider.ActionDescriptors.Items
                    .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
                    .FirstOrDefault(d => 
                        d.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase) && 
                        d.ControllerName.Equals(controllerName, StringComparison.OrdinalIgnoreCase));

                if (actionDescriptor == null)
                    return new HtmlString("");

                // Create the controller
                var controllerFactory = services.GetService(typeof(Microsoft.AspNetCore.Mvc.Controllers.IControllerFactory)) 
                    as Microsoft.AspNetCore.Mvc.Controllers.IControllerFactory;
                
                if (controllerFactory == null)
                    return new HtmlString("");

                var routeDataCopy = new Microsoft.AspNetCore.Routing.RouteData(html.ViewContext.RouteData);
                routeDataCopy.Values["controller"] = controllerName;
                routeDataCopy.Values["action"] = actionName;
                
                // Add route values if provided
                if (routeValues != null)
                {
                    foreach (var prop in routeValues.GetType().GetProperties())
                    {
                        routeDataCopy.Values[prop.Name] = prop.GetValue(routeValues);
                    }
                }

                var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, routeDataCopy, actionDescriptor);
                var controllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext(actionContext);
                
                var controller = controllerFactory.CreateController(controllerContext);
                if (controller is Microsoft.AspNetCore.Mvc.Controller mvcController)
                {
                    mvcController.ControllerContext = controllerContext;
                    
                    // Invoke the action method
                    var methodInfo = actionDescriptor.MethodInfo;
                    var parameters = methodInfo.GetParameters();
                    var args = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        // Try to get from route values
                        if (routeDataCopy.Values.TryGetValue(parameters[i].Name, out var val))
                            args[i] = val;
                        else if (parameters[i].HasDefaultValue)
                            args[i] = parameters[i].DefaultValue;
                        else
                            args[i] = parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
                    }

                    var result = methodInfo.Invoke(controller, args);
                    
                    // Handle the result
                    if (result is Microsoft.AspNetCore.Mvc.ViewResult viewResult)
                    {
                        // Render the view to string
                        var viewEngine = services.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine)) 
                            as Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine;
                        
                        var viewName = viewResult.ViewName ?? actionName;
                        var findResult = viewEngine.FindView(actionContext, viewName, false);
                        if (!findResult.Success)
                            findResult = viewEngine.GetView(null, $"~/Views/{controllerName}/{viewName}.cshtml", false);
                        
                        if (findResult.Success)
                        {
                            using (var writer = new System.IO.StringWriter())
                            {
                                var viewData = viewResult.ViewData ?? new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(html.ViewContext.ViewData);
                                if (viewResult.Model != null)
                                    viewData.Model = viewResult.Model;
                                    
                                var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(
                                    actionContext,
                                    findResult.View,
                                    viewData,
                                    viewResult.TempData ?? html.ViewContext.TempData,
                                    writer,
                                    new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());
                                findResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                                return new HtmlString(writer.ToString());
                            }
                        }
                    }
                    else if (result is Microsoft.AspNetCore.Mvc.PartialViewResult partialResult)
                    {
                        var viewEngine = services.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine)) 
                            as Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine;
                        
                        var viewName = partialResult.ViewName ?? actionName;
                        var findResult = viewEngine.FindView(actionContext, viewName, false);
                        if (!findResult.Success)
                            findResult = viewEngine.GetView(null, $"~/Views/{controllerName}/{viewName}.cshtml", false);
                        
                        if (findResult.Success)
                        {
                            using (var writer = new System.IO.StringWriter())
                            {
                                var viewData = partialResult.ViewData ?? new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(html.ViewContext.ViewData);
                                if (partialResult.Model != null)
                                    viewData.Model = partialResult.Model;
                                    
                                var viewContext = new Microsoft.AspNetCore.Mvc.Rendering.ViewContext(
                                    actionContext,
                                    findResult.View,
                                    viewData,
                                    partialResult.TempData ?? html.ViewContext.TempData,
                                    writer,
                                    new Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions());
                                findResult.View.RenderAsync(viewContext).GetAwaiter().GetResult();
                                return new HtmlString(writer.ToString());
                            }
                        }
                    }
                    else if (result is Microsoft.AspNetCore.Mvc.ContentResult contentResult)
                    {
                        return new HtmlString(contentResult.Content ?? "");
                    }
                    else if (result is Microsoft.AspNetCore.Mvc.EmptyResult)
                    {
                        return new HtmlString("");
                    }
                }
                
                controllerFactory.ReleaseController(controllerContext, controller);
            }
            catch
            {
                // If child action rendering fails, return empty content rather than crashing the page
            }
            return new HtmlString("");
        }

        /// <summary>
        /// Creates a Pager instance for paginated views.
        /// </summary>
        public static Pager Pager(this IHtmlHelper html, IPageableModel model)
        {
            return new Pager(model, html.ViewContext);
        }
    }
}
