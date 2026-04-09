using Nop.Core.Configuration;
using Nop.Core.Domain.Seo;

namespace Nop.Core.Domain.Seo;

public class SeoSettings : ISettings
{
    public string? PageTitleSeparator { get; set; }
    public PageTitleSeoAdjustment PageTitleSeoAdjustment { get; set; }
    public string? DefaultTitle { get; set; }
    public string? DefaultMetaKeywords { get; set; }
    public string? DefaultMetaDescription { get; set; }
    public bool GenerateProductMetaDescription { get; set; }
    public bool ConvertNonWesternChars { get; set; }
    public bool AllowUnicodeCharsInUrls { get; set; }
    public bool CanonicalUrlsEnabled { get; set; }
    public WwwRequirement WwwRequirement { get; set; }
    public bool EnableJsBundling { get; set; }
    public bool EnableCssBundling { get; set; }
    public bool TwitterMetaTags { get; set; }
    public bool OpenGraphMetaTags { get; set; }
    public List<string> ReservedUrlRecordSlugs { get; set; } = [];
    public string? CustomHeadTags { get; set; }
}
