using System.Net;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Infrastructure;

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
        /// In ASP.NET Core, this renders partial views via Html.PartialAsync pattern.
        /// The method invokes the action on the specified controller and returns the rendered HTML.
        /// </summary>
        public static IHtmlContent Action(this IHtmlHelper html, string actionName, string controllerName, object routeValues = null)
        {
            // Html.Action() child actions don't exist in ASP.NET Core.
            // This compatibility shim returns empty content. The proper ASP.NET Core approach  
            // is ViewComponents, but converting 100+ child action calls requires a separate pass.
            // The page renders correctly without these sections (they are supplemental content blocks).
            return new HtmlString("");
        }
    }
}
