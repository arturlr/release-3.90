using System;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Nop.Web.Framework.Localization
{
    /// <summary>
    /// A localized string that renders as raw markup.
    /// </summary>
    /// <remarks>
    /// Ported in task 6.3 from <c>System.Web.IHtmlString</c> to
    /// <see cref="IHtmlContent"/> (<c>Microsoft.AspNetCore.Html</c>).
    ///
    /// Task 6.2 correctly observed that this file compiled unchanged on net10.0, because
    /// <c>System.Web.IHtmlString</c> and <c>System.Web.HtmlString</c> still ship in the in-box
    /// <c>System.Web.HttpUtility</c> assembly. It was left for 6.3 to decide, and the decision is
    /// to move it: ASP.NET Core's Razor engine, <c>TagBuilder</c> and <c>IHtmlContentBuilder</c>
    /// only recognise <see cref="IHtmlContent"/>. A <c>LocalizedString</c> that implemented only
    /// the legacy interface would be written through <c>object.ToString()</c> and then
    /// HTML-ENCODED by Razor, silently double-encoding every localized resource that contains
    /// markup or an apostrophe.
    ///
    /// The instance method <c>ToHtmlString()</c> is retained (it is no longer an interface
    /// implementation) so the many <c>T("...").ToHtmlString()</c> call sites in the Nop.Web and
    /// Nop.Admin views keep compiling. <see cref="WriteTo"/> writes the value verbatim, which is
    /// what <c>IHtmlString</c> meant; <paramref name="encoder"/> is intentionally unused.
    /// </remarks>
    public class LocalizedString : MarshalByRefObject, IHtmlContent
    {
        private readonly string _localized;
        private readonly string _scope;
        private readonly string _textHint;
        private readonly object[] _args;

        public LocalizedString(string localized)
        {
            _localized = localized;
        }

        public LocalizedString(string localized, string scope, string textHint, object[] args)
        {
            _localized = localized;
            _scope = scope;
            _textHint = textHint;
            _args = args;
        }

        public static LocalizedString TextOrDefault(string text, LocalizedString defaultValue)
        {
            if (string.IsNullOrEmpty(text))
                return defaultValue;
            return new LocalizedString(text);
        }

        public string Scope
        {
            get { return _scope; }
        }

        public string TextHint
        {
            get { return _textHint; }
        }

        public object[] Args
        {
            get { return _args; }
        }

        public string Text
        {
            get { return _localized; }
        }

        public override string ToString()
        {
            return _localized;
        }

        public string ToHtmlString()
        {
            return _localized;
        }

        /// <summary>
        /// Write the localized value as raw markup.
        /// </summary>
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            if (_localized != null)
                writer.Write(_localized);
        }

        public override int GetHashCode()
        {
            var hashCode = 0;
            if (_localized != null)
                hashCode ^= _localized.GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            var that = (LocalizedString)obj;
            return string.Equals(_localized, that._localized);
        }

    }
}
