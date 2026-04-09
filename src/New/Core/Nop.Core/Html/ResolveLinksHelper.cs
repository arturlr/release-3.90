using System.Globalization;
using System.Text.RegularExpressions;

namespace Nop.Core.Html;

/// <summary>
/// Auto-links URLs found in plain text
/// </summary>
public static partial class ResolveLinksHelper
{
    [GeneratedRegex(@"((http://|https://|www\.)([A-Z0-9.\-]{1,})\.[0-9A-Z?;~&\(\)#,=\-_\./\+]{2,})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    private const string LinkTemplate = "<a href=\"{0}{1}\" rel=\"nofollow\">{2}</a>";
    private const int MaxLength = 50;

    public static string FormatText(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        foreach (Match match in UrlRegex().Matches(text))
        {
            var prefix = match.Value.Contains("://") ? string.Empty : "http://";
            text = text.Replace(match.Value,
                string.Format(CultureInfo.InvariantCulture, LinkTemplate, prefix, match.Value, ShortenUrl(match.Value, MaxLength)));
        }
        return text;
    }

    private static string ShortenUrl(string url, int max)
    {
        if (url.Length <= max)
            return url;

        var startIndex = url.IndexOf("://", StringComparison.Ordinal);
        if (startIndex > -1)
            url = url[(startIndex + 3)..];

        if (url.Length <= max) return url;

        var firstIndex = url.IndexOf('/') + 1;
        var lastIndex = url.LastIndexOf('/');
        if (firstIndex < lastIndex)
        {
            url = url.Remove(firstIndex, lastIndex - firstIndex);
            url = url.Insert(firstIndex, "...");
        }

        if (url.Length <= max) return url;

        var queryIndex = url.IndexOf('?');
        if (queryIndex > -1) url = url[..queryIndex];
        if (url.Length <= max) return url;

        var fragmentIndex = url.IndexOf('#');
        if (fragmentIndex > -1) url = url[..fragmentIndex];
        if (url.Length <= max) return url;

        firstIndex = url.LastIndexOf('/') + 1;
        lastIndex = url.LastIndexOf('.');
        if (lastIndex - firstIndex > 10)
        {
            var page = url[firstIndex..lastIndex];
            var length = url.Length - max + 3;
            if (page.Length > length)
                url = url.Replace(page, "..." + page[length..]);
        }
        return url;
    }
}
