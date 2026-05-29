using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core.Infrastructure;

namespace Nop.Web.Framework.UI
{
    /// <summary>
    /// Layout extensions
    /// </summary>
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
            return new HtmlString(System.Net.WebUtility.HtmlEncode(pageHeadBuilder.GenerateTitle(addDefaultTitle)));
        }

        public static void AddMetaDescriptionParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddMetaDescriptionParts(part); }
        public static void AppendMetaDescriptionParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendMetaDescriptionParts(part); }
        public static IHtmlContent NopMetaDescription(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendMetaDescriptionParts(part);
            return new HtmlString(System.Net.WebUtility.HtmlEncode(pageHeadBuilder.GenerateMetaDescription()));
        }

        public static void AddMetaKeywordParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddMetaKeywordParts(part); }
        public static void AppendMetaKeywordParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendMetaKeywordParts(part); }
        public static IHtmlContent NopMetaKeywords(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendMetaKeywordParts(part);
            return new HtmlString(System.Net.WebUtility.HtmlEncode(pageHeadBuilder.GenerateMetaKeywords()));
        }

        public static void AddScriptParts(this IHtmlHelper html, string part, bool excludeFromBundle = false, bool isAsync = false) => AddScriptParts(html, ResourceLocation.Head, part, excludeFromBundle, isAsync);
        public static void AddScriptParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false, bool isAsync = false) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddScriptParts(location, part, excludeFromBundle, isAsync); }
        public static void AppendScriptParts(this IHtmlHelper html, string part, bool excludeFromBundle = false, bool isAsync = false) => AppendScriptParts(html, ResourceLocation.Head, part, excludeFromBundle, isAsync);
        public static void AppendScriptParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false, bool isAsync = false) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendScriptParts(location, part, excludeFromBundle, isAsync); }
        public static IHtmlContent NopScripts(this IHtmlHelper html, ResourceLocation location, bool? bundleFiles = null)
        {
            return new HtmlString(EngineContext.Current.Resolve<IPageHeadBuilder>().GenerateScripts(location, bundleFiles));
        }

        public static void AddCssFileParts(this IHtmlHelper html, string part, bool excludeFromBundle = false) => AddCssFileParts(html, ResourceLocation.Head, part, excludeFromBundle);
        public static void AddCssFileParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddCssFileParts(location, part, excludeFromBundle); }
        public static void AppendCssFileParts(this IHtmlHelper html, string part, bool excludeFromBundle = false) => AppendCssFileParts(html, ResourceLocation.Head, part, excludeFromBundle);
        public static void AppendCssFileParts(this IHtmlHelper html, ResourceLocation location, string part, bool excludeFromBundle = false) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendCssFileParts(location, part, excludeFromBundle); }
        public static IHtmlContent NopCssFiles(this IHtmlHelper html, ResourceLocation location, bool? bundleFiles = null)
        {
            return new HtmlString(EngineContext.Current.Resolve<IPageHeadBuilder>().GenerateCssFiles(location, bundleFiles));
        }

        public static void AddCanonicalUrlParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddCanonicalUrlParts(part); }
        public static void AppendCanonicalUrlParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendCanonicalUrlParts(part); }
        public static IHtmlContent NopCanonicalUrls(this IHtmlHelper html, string part = "")
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendCanonicalUrlParts(part);
            return new HtmlString(pageHeadBuilder.GenerateCanonicalUrls());
        }

        public static void AddHeadCustomParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddHeadCustomParts(part); }
        public static void AppendHeadCustomParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendHeadCustomParts(part); }
        public static IHtmlContent NopHeadCustom(this IHtmlHelper html) { return new HtmlString(EngineContext.Current.Resolve<IPageHeadBuilder>().GenerateHeadCustom()); }

        public static void AddPageCssClassParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AddPageCssClassParts(part); }
        public static void AppendPageCssClassParts(this IHtmlHelper html, string part) { EngineContext.Current.Resolve<IPageHeadBuilder>().AppendPageCssClassParts(part); }
        public static IHtmlContent NopPageCssClasses(this IHtmlHelper html, string part = "", bool includeClassElement = true)
        {
            var pageHeadBuilder = EngineContext.Current.Resolve<IPageHeadBuilder>();
            html.AppendPageCssClassParts(part);
            var classes = pageHeadBuilder.GeneratePageCssClasses();
            if (string.IsNullOrEmpty(classes)) return null;
            var result = includeClassElement ? $"class=\"{classes}\"" : classes;
            return new HtmlString(result);
        }

        public static void SetActiveMenuItemSystemName(this IHtmlHelper html, string systemName) { EngineContext.Current.Resolve<IPageHeadBuilder>().SetActiveMenuItemSystemName(systemName); }
        public static string GetActiveMenuItemSystemName(this IHtmlHelper html) { return EngineContext.Current.Resolve<IPageHeadBuilder>().GetActiveMenuItemSystemName(); }
    }
}
