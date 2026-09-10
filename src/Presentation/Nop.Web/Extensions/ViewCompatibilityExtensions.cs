using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Nop.Web.Framework;
using Nop.Web.Framework.UI.Paging;

namespace Nop.Web.Extensions
{
    /// <summary>
    /// Small shims for three MVC 5 view idioms that ASP.NET Core has no direct equivalent for
    /// (task 7.3, Requirement 4.4).
    /// </summary>
    /// <remarks>
    /// Each of these replaces a construct used in dozens of <c>Nop.Web</c> views. They live here
    /// rather than being inlined at every call site so that the reasoning is stated once and the
    /// views stay readable.
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
