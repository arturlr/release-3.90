using System.Text.RegularExpressions;

namespace Nop.Core.Html;

/// <summary>
/// BBCode to HTML converter
/// </summary>
public static partial class BBCodeHelper
{
    [GeneratedRegex(@"\[b\](.+?)\[/b\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"\[i\](.+?)\[/i\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"\[u\](.+?)\[/u\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UnderlineRegex();

    [GeneratedRegex(@"\[url\=([^\]]+)\]([^\]]+)\[/url\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Url1Regex();

    [GeneratedRegex(@"\[url\](.+?)\[/url\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Url2Regex();

    [GeneratedRegex(@"\[quote=(.+?)\](.+?)\[/quote\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex QuoteRegex();

    [GeneratedRegex(@"\[img\](.+?)\[/img\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ImgRegex();

    [GeneratedRegex(@"\[quote=(.+?)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex QuoteOpenRegex();

    [GeneratedRegex(@"\[/quote\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex QuoteCloseRegex();

    /// <param name="openLinksInNewWindow">
    /// Replaces legacy EngineContext.Current.Resolve&lt;CommonSettings&gt;().BbcodeEditorOpenLinksInNewWindow
    /// </param>
    public static string FormatText(string? text, bool replaceBold, bool replaceItalic,
        bool replaceUnderline, bool replaceUrl, bool replaceCode, bool replaceQuote, bool replaceImg,
        bool openLinksInNewWindow = false)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        if (replaceBold)
            text = BoldRegex().Replace(text, "<strong>$1</strong>");

        if (replaceItalic)
            text = ItalicRegex().Replace(text, "<em>$1</em>");

        if (replaceUnderline)
            text = UnderlineRegex().Replace(text, "<u>$1</u>");

        if (replaceUrl)
        {
            var target = openLinksInNewWindow ? " target=_blank" : "";
            text = Url1Regex().Replace(text, $"<a href=\"$1\" rel=\"nofollow\"{target}>$2</a>");
            text = Url2Regex().Replace(text, $"<a href=\"$1\" rel=\"nofollow\"{target}>$1</a>");
        }

        if (replaceQuote)
        {
            while (QuoteRegex().IsMatch(text))
                text = QuoteRegex().Replace(text, "<b>$1 wrote:</b><div class=\"quote\">$2</div>");
        }

        if (replaceImg)
            text = ImgRegex().Replace(text, "<img src=\"$1\" class=\"user-posted-image\" alt=\"\">");

        return text;
    }

    public static string RemoveQuotes(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        str = QuoteOpenRegex().Replace(str, string.Empty);
        str = QuoteCloseRegex().Replace(str, string.Empty);
        return str;
    }
}
