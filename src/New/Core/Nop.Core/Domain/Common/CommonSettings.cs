using Nop.Core.Configuration;

namespace Nop.Core.Domain.Common;

public class CommonSettings : ISettings
{
    public bool SubjectFieldOnContactUsForm { get; set; }
    public bool UseSystemEmailForContactUsForm { get; set; }
    public bool UseStoredProceduresIfSupported { get; set; }
    public bool UseStoredProcedureForLoadingCategories { get; set; }
    public bool SitemapEnabled { get; set; }
    public bool SitemapIncludeCategories { get; set; }
    public bool SitemapIncludeManufacturers { get; set; }
    public bool SitemapIncludeProducts { get; set; }
    public List<string> SitemapCustomUrls { get; set; } = [];
    public bool DisplayJavaScriptDisabledWarning { get; set; }
    public bool UseFullTextSearch { get; set; }
    public FulltextSearchMode FullTextMode { get; set; }
    public bool Log404Errors { get; set; }
    public string? BreadcrumbDelimiter { get; set; }
    public bool RenderXuaCompatible { get; set; }
    public string? XuaCompatibleValue { get; set; }
    public List<string> IgnoreLogWordlist { get; set; } = [];
    public bool BbcodeEditorOpenLinksInNewWindow { get; set; }
}
