namespace Nop.Services.Seo;

/// <summary>
/// Sitemap generator — builds XML sitemaps for search engine indexing.
/// Implementation deferred to Phase 4 (depends on ICategoryService, IProductService, etc.)
/// </summary>
public interface ISitemapGenerator
{
    Task<string> GenerateAsync(int? id);
    Task GenerateAsync(Stream stream, int? id);
}
