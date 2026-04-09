using System.Collections.Frozen;
using Microsoft.AspNetCore.Http;

namespace Nop.Services.Helpers;

/// <summary>
/// Detects search engine crawlers via user agent substring matching.
/// Replaces legacy BrowscapXmlHelper (heavy XML parsing) with a lightweight frozen set of known crawler tokens.
/// </summary>
public class UserAgentHelper : IUserAgentHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Well-known crawler user agent substrings (case-insensitive match)
    private static readonly FrozenSet<string> CrawlerTokens = new[]
    {
        "bot", "crawl", "spider", "slurp", "mediapartners", "googlebot",
        "bingbot", "yandex", "baiduspider", "duckduckbot", "facebookexternalhit",
        "twitterbot", "rogerbot", "linkedinbot", "embedly", "showyoubot",
        "outbrain", "pinterest", "applebot", "semrushbot", "ahrefsbot",
        "mj12bot", "dotbot", "petalbot", "bytespider", "gptbot"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public UserAgentHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public virtual bool IsSearchEngine()
    {
        var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(userAgent))
            return false;

        foreach (var token in CrawlerTokens)
        {
            if (userAgent.Contains(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
