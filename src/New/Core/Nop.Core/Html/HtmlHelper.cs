using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Nop.Core.Html;

/// <summary>
/// HTML formatting and sanitization utilities
/// </summary>
public static partial class HtmlHelper
{
    private static readonly string[] AllowedTags =
        "br,hr,b,i,u,a,div,ol,ul,li,blockquote,img,span,p,em,strong,font,pre,h1,h2,h3,h4,h5,h6,address,cite".Split(',');

    [GeneratedRegex("<p>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ParagraphStartRegex();

    [GeneratedRegex("</p>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ParagraphEndRegex();

    [GeneratedRegex("<.*?>", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"(>)(\r|\n)*(<)")]
    private static partial Regex TagWhitespaceRegex();

    [GeneratedRegex("(<[^>]*>)([^<]*)")]
    private static partial Regex TagContentRegex();

    [GeneratedRegex(@"(&#x?[0-9]{2,4};|&quot;|&amp;|&nbsp;|&lt;|&gt;|&euro;|&copy;|&reg;|&permil;|&Dagger;|&dagger;|&lsaquo;|&rsaquo;|&bdquo;|&rdquo;|&ldquo;|&sbquo;|&rsquo;|&lsquo;|&mdash;|&ndash;|&rlm;|&lrm;|&zwj;|&zwnj;|&thinsp;|&emsp;|&ensp;|&tilde;|&circ;|&Yuml;|&scaron;|&Scaron;)")]
    private static partial Regex HtmlEntityRegex();

    [GeneratedRegex(@"<a\b[^>]+>([^<]*(?:(?!</a)<[^<]*)*)</a>", RegexOptions.IgnoreCase)]
    private static partial Regex AnchorTagRegex();

    public static string FormatText(string? text, bool stripTags,
        bool convertPlainTextToHtml, bool allowHtml,
        bool allowBBCode, bool resolveLinks, bool addNoFollowTag)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        try
        {
            if (stripTags)
                text = StripTags(text);

            text = allowHtml ? EnsureOnlyAllowedHtml(text) : WebUtility.HtmlEncode(text);

            if (convertPlainTextToHtml)
                text = ConvertPlainTextToHtml(text);

            if (allowBBCode)
                text = BBCodeHelper.FormatText(text, true, true, true, true, true, true, true);

            if (resolveLinks)
                text = ResolveLinksHelper.FormatText(text);
        }
        catch (Exception exc)
        {
            text = $"Text cannot be formatted. Error: {exc.Message}";
        }
        return text;
    }

    public static string StripTags(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = TagWhitespaceRegex().Replace(text, "><");
        text = TagContentRegex().Replace(text, "$2");
        text = HtmlEntityRegex().Replace(text, "@");
        return text;
    }

    public static string ReplaceAnchorTags(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        return AnchorTagRegex().Replace(text, "$1");
    }

    public static string ConvertPlainTextToHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = text.Replace("\r\n", "<br />");
        text = text.Replace("\r", "<br />");
        text = text.Replace("\n", "<br />");
        text = text.Replace("\t", "&nbsp;&nbsp;");
        text = text.Replace("  ", "&nbsp;&nbsp;");
        return text;
    }

    public static string ConvertHtmlToPlainText(string? text, bool decode = false, bool replaceAnchorTags = false)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (decode)
            text = WebUtility.HtmlDecode(text);

        text = text.Replace("<br>", "\n");
        text = text.Replace("<br >", "\n");
        text = text.Replace("<br />", "\n");
        text = text.Replace("&nbsp;&nbsp;", "\t");
        text = text.Replace("&nbsp;&nbsp;", "  ");

        if (replaceAnchorTags)
            text = ReplaceAnchorTags(text);

        return text;
    }

    public static string ConvertPlainTextToParagraph(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = ParagraphStartRegex().Replace(text, string.Empty);
        text = ParagraphEndRegex().Replace(text, "\n");
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        text += "\n\n";
        text = text.Replace("\n\n", "\n");

        var sb = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                sb.AppendFormat("<p>{0}</p>\n", line);
        }
        return sb.ToString();
    }

    private static string EnsureOnlyAllowedHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var matches = HtmlTagRegex().Matches(text);
        for (var i = matches.Count - 1; i >= 0; i--)
        {
            var tag = text.Substring(matches[i].Index + 1, matches[i].Length - 1).Trim().ToLowerInvariant();
            if (!IsValidTag(tag))
                text = text.Remove(matches[i].Index, matches[i].Length);
        }
        return text;
    }

    private static bool IsValidTag(string tag)
    {
        if (tag.Contains("javascript") || tag.Contains("vbscript") || tag.Contains("onclick"))
            return false;

        var endChars = new[] { ' ', '>', '/', '\t' };
        var pos = tag.IndexOfAny(endChars, 1);
        if (pos > 0) tag = tag[..pos];
        if (tag[0] == '/') tag = tag[1..];

        return AllowedTags.Contains(tag);
    }
}
