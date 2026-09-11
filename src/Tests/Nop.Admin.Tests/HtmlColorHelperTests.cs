using NUnit.Framework;
using Nop.Admin.Helpers;

namespace Nop.Admin.Tests
{
    /// <summary>
    /// TASK 8.6 - the <c>System.Drawing.ColorTranslator.FromHtml</c> -> ImageSharp
    /// <c>Color.TryParse</c> replacement behind the four <c>ColorSquaresRgb</c> validations in
    /// <c>ProductController</c> and <c>CheckoutAttributeController</c>.
    /// </summary>
    /// <remarks>
    /// The parity matrix and its 11 differences are recorded on
    /// <see cref="HtmlColorHelper"/>. These tests pin BOTH directions - what must still be
    /// accepted, what is now correctly rejected, and the single loosening - so a later change
    /// cannot quietly move any of them.
    /// </remarks>
    [TestFixture]
    public class HtmlColorHelperTests
    {
        // ---- unchanged: everything the admin UI can actually produce ----

        [TestCase("#FF0000")]
        [TestCase("#ff0000")]
        [TestCase("#f00")]
        [TestCase("#F00")]
        [TestCase("Red")]
        [TestCase("red")]
        [TestCase("RED")]
        [TestCase("White")]
        [TestCase("Black")]
        [TestCase("DarkSlateGray")]
        [TestCase("darkslategray")]
        [TestCase("Transparent")]
        public void Task_8_6_the_colours_3_90_accepted_are_still_accepted(string value)
        {
            Assert.IsNull(HtmlColorHelper.ValidateHtmlColor(value),
                "'" + value + "' parsed under ColorTranslator.FromHtml and must still parse");
            Assert.IsTrue(HtmlColorHelper.IsValidHtmlColor(value));
        }

        [Test]
        public void Task_8_6_the_farbtastic_colour_picker_format_is_accepted()
        {
            //the admin views wire $('#color-picker').farbtastic(...), which writes #rrggbb
            Assert.IsNull(HtmlColorHelper.ValidateHtmlColor("#3f7ab5"));
        }

        // ---- unchanged: what 3.90 rejected and is still rejected ----

        [TestCase("notacolour")]
        [TestCase("#GGGGGG")]
        [TestCase("#")]
        [TestCase("rgb(255,0,0)")]
        public void Task_8_6_what_3_90_rejected_is_still_rejected(string value)
        {
            Assert.IsNotNull(HtmlColorHelper.ValidateHtmlColor(value),
                "'" + value + "' threw under ColorTranslator.FromHtml and must still be refused");
        }

        // ---- TIGHTENED: 3.90 accepted these and stored them as CSS colours ----

        [TestCase("ButtonFace", TestName = "Windows system colour name")]
        [TestCase("ActiveBorder", TestName = "Windows system colour name (2)")]
        [TestCase("Menu", TestName = "Windows system colour name (3)")]
        [TestCase("255,0,0", TestName = "non-CSS comma form")]
        [TestCase("0xFF0000", TestName = "non-CSS 0x form")]
        [TestCase("#12345", TestName = "malformed hex, 5 digits")]
        [TestCase("#1234567", TestName = "malformed hex, 7 digits")]
        [TestCase(" ", TestName = "whitespace")]
        public void Task_8_6_the_parse_is_now_STRICTER_than_ColorTranslator_was(string value)
        {
            //ColorTranslator.FromHtml accepted every one of these - "#12345" became 01234500 and
            //" " became Color.Empty, both saved verbatim into markup where they are invalid CSS.
            //Each rejection is an improvement, recorded rather than assumed.
            Assert.IsNotNull(HtmlColorHelper.ValidateHtmlColor(value),
                "'" + value + "' is not a usable CSS colour and should now be refused");
        }

        // ---- LOOSENED: the one difference in the other direction ----

        [TestCase("FF0000")]
        [TestCase("f00")]
        public void Task_8_6_KNOWN_LOOSENING_bare_hex_without_a_hash_is_now_accepted(string value)
        {
            //ImageSharp's Color.TryParse accepts hex without the leading '#'; ColorTranslator
            //threw ArgumentException. Consequence: an admin who types over the colour picker's
            //value can now save "FF0000", which is not a valid CSS colour, so the square renders
            //colourless instead of showing a validation error. Pinned so the difference is
            //visible and a future decision to tighten it is a deliberate test change, not a
            //silent one.
            Assert.IsNull(HtmlColorHelper.ValidateHtmlColor(value),
                "recorded loosening - see HtmlColorHelper's remarks");
        }

        // ---- the null/empty contract, which keeps the model-error count at 3.90's ----

        [TestCase(null)]
        [TestCase("")]
        public void Task_8_6_null_and_empty_are_the_callers_required_case_not_a_parse_failure(string value)
        {
            //All four call sites already add "Color is required" from their own IsNullOrEmpty
            //test, and ColorTranslator.FromHtml("") returned Color.Empty without throwing - so
            //3.90 produced exactly ONE model error for an empty value. Returning null here keeps
            //it at one instead of two.
            Assert.IsNull(HtmlColorHelper.ValidateHtmlColor(value));
        }

        [Test]
        public void Task_8_6_the_validation_message_names_the_offending_value()
        {
            var message = HtmlColorHelper.ValidateHtmlColor("notacolour");
            Assert.IsNotNull(message);
            Assert.That(message, Does.Contain("notacolour"));
        }
    }
}
