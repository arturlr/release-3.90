using System;

namespace Nop.Admin.Helpers
{
    /// <summary>
    /// Validation of the HTML colour strings stored in <c>ColorSquaresRgb</c>.
    /// </summary>
    /// <remarks>
    /// TASK 8.6 (design section 7, Requirements 5.8, 5.9, 5.11). This replaces the four
    /// identical <c>System.Drawing.ColorTranslator.FromHtml(model.ColorSquaresRgb)</c>
    /// "can it be instanciated?" probes in <c>ProductController</c> (2) and
    /// <c>CheckoutAttributeController</c> (2). Those are not an imaging pipeline at all -
    /// they are a colour-string parse - so design section 7 maps them onto
    /// <see cref="SixLabors.ImageSharp.Color.TryParse(string, out SixLabors.ImageSharp.Color)"/>
    /// rather than onto an ImageSharp image operation.
    ///
    /// WHY IT HAD TO MOVE AT ALL. <c>ColorTranslator</c> lives in the Windows-only
    /// <c>System.Drawing.Common</c> package, which reaches this project's compile graph only
    /// transitively (EPPlus 4.5.3.3 -> System.Drawing.Common 4.7.2, pinned centrally by task
    /// 4.2 section 10 to clear NU1904 / CVE-2021-24112). Measured on Linux with that exact
    /// pin: <c>ColorTranslator.FromHtml("#ff0000")</c> actually WORKS, because the 4.7.x line
    /// resolves named colours in managed code and only reaches libgdiplus for real imaging -
    /// so unlike RoxyFilemanController's Bitmap/Graphics usage these four sites were not
    /// throwing today. They are migrated anyway because keeping them is the last thing
    /// binding a Windows-only package for a task a cross-platform library does natively, and
    /// because the parse is measurably STRICTER afterwards (below).
    ///
    /// PARITY, MEASURED - <c>ColorTranslator.FromHtml</c> vs <c>Color.TryParse</c> over a
    /// 33-input matrix. Identical for everything the admin UI can actually produce: the
    /// farbtastic colour picker in <c>Product/_CreateOrUpdateProductAttributeValue.cshtml</c>
    /// and <c>CheckoutAttribute/_CreateOrUpdateValue.cshtml</c> emits <c>#rrggbb</c>, and
    /// <c>#rrggbb</c>, <c>#rgb</c> and every CSS named colour (any casing, including
    /// <c>rebeccapurple</c>) parse to the same value under both. Eleven inputs differ:
    ///
    ///   TIGHTENED - now rejected, previously accepted (all 8 are improvements; each one
    ///   used to be stored verbatim into markup as a CSS colour, where it is invalid):
    ///     "ButtonFace", "ActiveBorder", "Menu"  Windows SYSTEM colour names - OS theme
    ///                                           colours, meaningless in a colour square
    ///     "255,0,0", "0xFF0000"                 non-CSS numeric forms
    ///     "#12345", "#1234567"                  malformed hex that ColorTranslator silently
    ///                                           accepted and mangled ("#12345" became
    ///                                           01234500)
    ///     " "                                   whitespace, which ColorTranslator returned
    ///                                           as Color.Empty without throwing, so 3.90
    ///                                           saved it as a valid colour
    ///
    ///   LOOSENED - now accepted, previously rejected. ONE input, and it is recorded rather
    ///   than defended:
    ///     "FF0000", "f00"                       hex WITHOUT the leading '#'. ImageSharp
    ///                                           accepts bare hex; ColorTranslator threw
    ///                                           ArgumentException. Consequence: an admin who
    ///                                           hand-types "FF0000" now passes validation and
    ///                                           the value is stored without a '#', which is
    ///                                           not a valid CSS colour, so the square renders
    ///                                           without colour instead of showing a
    ///                                           validation error. Only reachable by typing
    ///                                           over the colour picker's value.
    ///
    /// The empty/null case is deliberately excluded here so the aggregate behaviour is
    /// unchanged: all four call sites already emit "Color is required" from their own
    /// <c>String.IsNullOrEmpty</c> test immediately above, and <c>FromHtml("")</c> returned
    /// <c>Color.Empty</c> without throwing, so 3.90 produced exactly ONE model error for an
    /// empty value. Returning null for null/empty keeps it at one instead of two.
    /// </remarks>
    public static class HtmlColorHelper
    {
        /// <summary>
        /// Validates an HTML colour string.
        /// </summary>
        /// <param name="value">The colour string, e.g. <c>#RRGGBB</c>, <c>#RGB</c> or a CSS colour name</param>
        /// <returns>
        /// <c>null</c> when the value is valid, or when it is null/empty (which the caller
        /// reports separately as "required"); otherwise the validation message to add to
        /// <c>ModelState</c>.
        /// </returns>
        public static string ValidateHtmlColor(string value)
        {
            //null/empty is the caller's "Color is required" case - see the remarks
            if (string.IsNullOrEmpty(value))
                return null;

            SixLabors.ImageSharp.Color parsed;
            if (SixLabors.ImageSharp.Color.TryParse(value, out parsed))
                return null;

            return string.Format("\"{0}\" is not a valid color", value);
        }

        /// <summary>
        /// Gets a value indicating whether the string is a valid HTML colour.
        /// </summary>
        public static bool IsValidHtmlColor(string value)
        {
            SixLabors.ImageSharp.Color parsed;
            return SixLabors.ImageSharp.Color.TryParse(value, out parsed);
        }
    }
}
