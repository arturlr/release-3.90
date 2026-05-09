using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Nop.Services.Common
{
    /// <summary>
    /// Extension methods for itext7 layout elements to restore SetBold()/SetItalic() functionality
    /// that was available on Paragraph in earlier versions.
    /// </summary>
    internal static class ITextExtensions
    {
        /// <summary>
        /// Sets the paragraph text to bold by setting the BOLD_SIMULATION property.
        /// </summary>
        public static Paragraph SetBold(this Paragraph paragraph)
        {
            paragraph.SetProperty(Property.BOLD_SIMULATION, true);
            return paragraph;
        }

        /// <summary>
        /// Sets the paragraph text to italic by setting the ITALIC_SIMULATION property.
        /// </summary>
        public static Paragraph SetItalic(this Paragraph paragraph)
        {
            paragraph.SetProperty(Property.ITALIC_SIMULATION, true);
            return paragraph;
        }
    }
}
