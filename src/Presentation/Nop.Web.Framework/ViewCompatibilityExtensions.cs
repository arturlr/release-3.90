using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Nop.Core.Infrastructure;
using Nop.Web.Framework.UI.Paging;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Small shims for four MVC 5 view idioms that ASP.NET Core has no direct equivalent for
    /// (task 7.3, Requirement 4.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each of these replaces a construct used in dozens of views. They live here rather than
    /// being inlined at every call site so that the reasoning is stated once and the views stay
    /// readable.
    /// </para>
    /// <para>
    /// <b>TASK 8.4 — PROMOTED FROM <c>Nop.Web</c>, for the same reason and by the same mechanism
    /// task 8.3 promoted <c>ChildActionExtensions</c> (deferral 7.3-1).</b> This file was created
    /// by task 7.3 as <c>Nop.Web/Extensions/ViewCompatibilityExtensions.cs</c> in namespace
    /// <c>Nop.Web.Extensions</c>. <c>Nop.Admin</c> needs all four shims and does <b>not</b>
    /// reference <c>Nop.Web</c> — the two are documented siblings (design section 6) — so the
    /// alternative was a second copy. The namespace is <c>Nop.Web.Framework</c> rather than
    /// <c>Nop.Web.Framework.Mvc</c> because <c>@using Nop.Web.Framework</c> is already present in
    /// every <c>_ViewImports.cshtml</c> in the solution, so <b>no view call site in either project
    /// had to change</b> — <c>Nop.Web</c>'s views keep resolving through that import rather than
    /// through <c>@using Nop.Web.Extensions</c>. Bodies are unchanged apart from the namespace;
    /// <see cref="GetFullHtmlFieldId"/> is new and is described on itself.
    /// </para>
    /// </remarks>
    public static class ViewCompatibilityExtensions
    {
        /// <summary>
        /// Is this content null or empty markup? Replaces
        /// <c>System.Web.Mvc.MvcHtmlString.IsNullOrEmpty(...)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>MvcHtmlString</c> was a <c>string</c> wrapper, so testing it for emptiness was
        /// trivial. <see cref="IHtmlContent"/> is a <i>writer</i> — the markup does not exist
        /// until <see cref="IHtmlContent.WriteTo"/> has run — so emptiness can only be
        /// determined by rendering. That is what this does, via the
        /// <see cref="HtmlContentExtensions.ToHtmlString(IHtmlContent)"/> shim task 6.3 added.
        /// </para>
        /// <para>
        /// The 40 call sites are all of the form
        /// <c>@if (!MvcHtmlString.IsNullOrEmpty(validationSummary)) { … @validationSummary … }</c>
        /// — i.e. "only emit the wrapper markup when there is something to put in it". Rendering
        /// twice is a small cost; <see cref="Html.ValidationSummary(bool)"/> is cheap and
        /// side-effect free.
        /// </para>
        /// </remarks>
        /// <param name="content">Content to test</param>
        /// <returns>True when there is no markup to render</returns>
        public static bool IsNullOrEmpty(this IHtmlContent content)
        {
            return content == null || string.IsNullOrEmpty(content.ToHtmlString());
        }

        /// <summary>
        /// <see cref="IsNullOrEmpty(IHtmlContent)"/> for a <see cref="Pager"/>.
        /// </summary>
        /// <remarks>
        /// A more specific overload so the pager call sites use <see cref="Pager.IsEmpty"/>
        /// instead of rendering the pager twice — building a pager materialises every page link.
        /// Task 6.3 kept <c>Pager.IsEmpty()</c> for exactly this reason.
        /// </remarks>
        /// <param name="pager">Pager</param>
        /// <returns>True when the pager would render nothing</returns>
        public static bool IsNullOrEmpty(this Pager pager)
        {
            return pager == null || pager.IsEmpty();
        }

        /// <summary>
        /// A new, EMPTY <see cref="ViewDataDictionary"/>. Replaces MVC 5's
        /// <c>new ViewDataDictionary()</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ASP.NET Core's <see cref="ViewDataDictionary"/> has no parameterless constructor: it
        /// requires an <c>IModelMetadataProvider</c> and a <c>ModelStateDictionary</c>, because
        /// metadata is no longer reachable from a static ambient provider.
        /// </para>
        /// <para>
        /// This deliberately produces an <b>empty</b> dictionary rather than
        /// <c>new ViewDataDictionary(ViewData)</c>. The copy constructor would inherit the
        /// parent's entries <i>and</i> its <c>TemplateInfo.HtmlFieldPrefix</c>, where MVC 5's
        /// parameterless constructor gave the partial a clean slate. Several call sites
        /// (<c>dataDictAddress</c>, <c>dataDictAttributes</c>, …) then set
        /// <c>HtmlFieldPrefix</c> themselves, so inheriting one would produce doubled field
        /// names such as <c>BillingNewAddress.BillingNewAddress.FirstName</c>.
        /// </para>
        /// </remarks>
        /// <param name="helper">HTML helper</param>
        /// <returns>Empty view data</returns>
        public static ViewDataDictionary NewViewData(this IHtmlHelper helper)
        {
            return new ViewDataDictionary(helper.MetadataProvider, helper.ViewContext.ModelState);
        }

        /// <summary>
        /// The fully-qualified <c>id</c> attribute value for a field, given a partial field name.
        /// Replaces MVC 5's <c>System.Web.Mvc.TemplateInfo.GetFullHtmlFieldId(...)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>TASK 8.4.</b> ASP.NET Core's <see cref="TemplateInfo"/> kept
        /// <see cref="TemplateInfo.GetFullHtmlFieldName"/> but <b>dropped
        /// <c>GetFullHtmlFieldId</c></b>, because the name-to-id transformation is no longer a
        /// fixed rule — it depends on <c>HtmlHelperOptions.IdAttributeDotReplacement</c>, which is
        /// per-application configuration. All 15 call sites are the admin editor templates
        /// (<c>Shared/EditorTemplates/Date</c>, <c>DateTime</c>, <c>Decimal</c>, <c>Int32</c>,
        /// <c>MultiSelect</c>, <c>RichEditor</c>, … each of which needs the id to attach a Kendo
        /// widget or TinyMCE instance to the input it just rendered), and every one passes
        /// <c>string.Empty</c>.
        /// </para>
        /// <para>
        /// This is written as an <b>extension method with MVC 5's exact name and signature</b>
        /// specifically so those 15 call sites are byte-identical to 3.90 — the alternative was
        /// editing 13 template files to inline the composition, which is more diff for no gain.
        /// </para>
        /// <para>
        /// <b>The composition reproduces MVC 5's, and it was verified rather than assumed.</b>
        /// MVC 5 did <c>GetFullHtmlFieldName(name)</c> then
        /// <c>TagBuilder.CreateSanitizedId(fullName, "_")</c>.
        /// <see cref="TagBuilder.CreateSanitizedId"/> still exists on ASP.NET Core but gained a
        /// required second argument (task 6.3 §15e recorded the same finding, and measured that
        /// <c>CreateSanitizedId("Locales[0].Name", "_")</c> yields <c>Locales_0__Name</c> — exactly
        /// what MVC 5's <c>GetFullHtmlFieldId</c> produced). The replacement string is taken from
        /// the ambient <see cref="Microsoft.AspNetCore.Mvc.ViewFeatures.HtmlHelperOptions"/> when
        /// one is reachable, falling back to <c>"_"</c>, which is both MVC 5's fixed behaviour and
        /// the ASP.NET Core default — so a host that has not changed the option gets 3.90's ids,
        /// and one that has gets ids consistent with every other field on the page.
        /// </para>
        /// <para>
        /// An <b>empty</b> full name yields an empty id rather than <c>"_"</c>: that is what
        /// <c>CreateSanitizedId</c> itself does for an empty input, and it matters because an
        /// editor template invoked with no prefix at all should produce no id, not a stray one.
        /// </para>
        /// </remarks>
        /// <param name="templateInfo">Template info</param>
        /// <param name="partialFieldName">Partial field name, relative to the current prefix</param>
        /// <returns>Sanitized, fully-qualified element id</returns>
        public static string GetFullHtmlFieldId(this TemplateInfo templateInfo, string partialFieldName)
        {
            if (templateInfo == null)
                throw new ArgumentNullException(nameof(templateInfo));

            var fullName = templateInfo.GetFullHtmlFieldName(partialFieldName);
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;

            return TagBuilder.CreateSanitizedId(fullName, DefaultIdAttributeDotReplacement);
        }

        /// <summary>
        /// The configured <c>IdAttributeDotReplacement</c>, or MVC 5's fixed <c>"_"</c>.
        /// </summary>
        /// <remarks>
        /// Resolved through <see cref="EngineContext"/> rather than injected because
        /// <see cref="GetFullHtmlFieldId"/> extends <see cref="TemplateInfo"/>, which carries no
        /// service provider. The <c>try/catch</c> tolerates there being no engine yet (install
        /// mode) or no MVC options registered: the value is cosmetic-but-consistent, and an id
        /// computed with the default separator is strictly better than an exception thrown out of a
        /// view. Cached because <c>MvcViewOptions</c> is an application-wide singleton and this is
        /// otherwise a container resolve per rendered form field.
        /// </remarks>
        private static string _idAttributeDotReplacement;

        private static string DefaultIdAttributeDotReplacement
        {
            get
            {
                if (_idAttributeDotReplacement != null)
                    return _idAttributeDotReplacement;

                try
                {
                    var options = EngineContext.Current
                        .Resolve<IOptions<MvcViewOptions>>()?.Value?.HtmlHelperOptions;
                    if (!string.IsNullOrEmpty(options?.IdAttributeDotReplacement))
                        return _idAttributeDotReplacement = options.IdAttributeDotReplacement;
                }
                catch
                {
                    //no engine yet (install mode), or MVC options not registered - fall through
                    //WITHOUT caching, so a later call still gets the configured value
                }

                return "_";
            }
        }
    }

    /// <summary>
    /// Encodes a string for embedding in a JavaScript string literal — the replacement for
    /// <c>System.Web.HttpUtility.JavaScriptStringEncode</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Task 7.3. <c>HttpUtility</c> does exist on net10.0 (it ships in the in-box
    /// <c>System.Web.HttpUtility</c> assembly), but taking a dependency on it would put a
    /// <c>System.Web*</c> assembly reference back into <c>Nop.Web</c>, which the migration's
    /// verification step explicitly checks for. <see cref="JavaScriptEncoder"/> is the
    /// ASP.NET Core replacement and is in the shared framework.
    /// </para>
    /// <para>
    /// <b>Behaviour difference, safe in this direction:</b> <see cref="JavaScriptEncoder"/> is
    /// <i>more</i> aggressive than <c>JavaScriptStringEncode</c> — it escapes non-ASCII
    /// characters, <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c>, <c>'</c> and <c>"</c> as
    /// <c>\uXXXX</c> where the old method escaped only the quote/backslash/control set plus
    /// <c>&lt;</c>. The escaped forms are equivalent JavaScript, so the value a script sees is
    /// unchanged; only the bytes on the wire differ. All 16 call sites embed the result inside
    /// a single-quoted literal, which both encoders make safe.
    /// </para>
    /// <para>
    /// <c>HttpUtility.JavaScriptStringEncode(null)</c> returned an empty string;
    /// <c>JavaScriptEncoder.Encode(null)</c> throws. The null is handled here so the call sites
    /// keep their exact previous behaviour.
    /// </para>
    /// </remarks>
    public static class JavaScriptHelper
    {
        /// <summary>
        /// Encode a value for use inside a JavaScript string literal.
        /// </summary>
        /// <param name="value">Value; <c>null</c> yields an empty string</param>
        /// <returns>Encoded value, without surrounding quotes</returns>
        public static string Encode(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : JavaScriptEncoder.Default.Encode(value);
        }
    }
}
