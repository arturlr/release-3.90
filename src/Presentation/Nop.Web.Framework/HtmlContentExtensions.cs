using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Helpers that bridge the ASP.NET Core <see cref="IHtmlContent"/> model back to the
    /// <c>string</c>-based composition style that nopCommerce 3.90 inherited from
    /// <c>System.Web.Mvc.MvcHtmlString</c> (task 6.3, Requirement 4.4).
    /// </summary>
    /// <remarks>
    /// MVC 5's <c>MvcHtmlString</c> was a <c>string</c> wrapper: <c>ToString()</c> and
    /// <c>ToHtmlString()</c> both returned the markup, so nopCommerce composes markup by
    /// concatenating helper results into a <see cref="System.Text.StringBuilder"/> in dozens of
    /// places, in this project and in the Nop.Web / Nop.Admin views.
    ///
    /// ASP.NET Core's <see cref="IHtmlContent"/> is a *writer*, not a string: the markup only
    /// exists once <see cref="IHtmlContent.WriteTo"/> has run against a <see cref="TextWriter"/>
    /// and an <see cref="HtmlEncoder"/>. Calling <c>ToString()</c> on an arbitrary
    /// <see cref="IHtmlContent"/> (a <c>TagBuilder</c>, an <c>HtmlContentBuilder</c>, a buffered
    /// view result) does NOT yield markup — it yields the type name or an unrelated value. Every
    /// such site is a silent-wrong-output bug that compiles cleanly, which is why this shim
    /// exists instead of leaving <c>ToString()</c> calls in place.
    ///
    /// The extension is deliberately named <c>ToHtmlString</c> so that the ~hundreds of
    /// <c>@Html.Something().ToHtmlString()</c> call sites in the Nop.Web (task 7.3) and
    /// Nop.Admin (task 8.4) views keep compiling without edits.
    /// </remarks>
    public static class HtmlContentExtensions
    {
        /// <summary>
        /// Render an <see cref="IHtmlContent"/> to its markup string using
        /// <see cref="HtmlEncoder.Default"/>.
        /// </summary>
        /// <param name="content">Content to render; <c>null</c> renders as an empty string</param>
        /// <returns>Markup</returns>
        public static string ToHtmlString(this IHtmlContent content)
        {
            if (content == null)
                return string.Empty;

            //fast path: HtmlString already holds the markup
            if (content is HtmlString htmlString)
                return htmlString.Value ?? string.Empty;

            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Render a <see cref="TagBuilder"/> in the requested mode.
        /// </summary>
        /// <remarks>
        /// Replaces MVC 5's <c>TagBuilder.ToString(TagRenderMode)</c>, which does not exist in
        /// ASP.NET Core — the render mode moved onto the <see cref="TagBuilder.TagRenderMode"/>
        /// property. Note this mutates <paramref name="tagBuilder"/>'s render mode, exactly as
        /// setting the property directly would.
        /// </remarks>
        /// <param name="tagBuilder">Tag builder</param>
        /// <param name="renderMode">Render mode</param>
        /// <returns>Markup</returns>
        public static string ToHtmlString(this TagBuilder tagBuilder, TagRenderMode renderMode)
        {
            if (tagBuilder == null)
                return string.Empty;

            tagBuilder.TagRenderMode = renderMode;
            return ((IHtmlContent)tagBuilder).ToHtmlString();
        }
    }
}
