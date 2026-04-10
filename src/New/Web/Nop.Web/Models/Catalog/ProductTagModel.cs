using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Catalog;

public class ProductTagModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? SeName { get; set; }
    public int ProductCount { get; set; }
}

public class ProductsByTagModel : BaseNopEntityModel
{
    public string? TagName { get; set; }
    public string? TagSeName { get; set; }
    public List<ProductOverviewModel> Products { get; set; } = [];
    public CatalogPagingFilteringModel PagingFilteringContext { get; set; } = new();
}

public class PopularProductTagsModel : BaseNopModel
{
    public int TotalTags { get; set; }
    public List<ProductTagModel> Tags { get; set; } = [];

    public int GetFontSize(ProductTagModel tag)
    {
        if (Tags.Count == 0) return 100;
        var weights = Tags.Select(t => (double)t.ProductCount).ToList();
        var mean = weights.Average();
        var stdDev = Math.Sqrt(weights.Sum(w => (w - mean) * (w - mean)) / weights.Count);
        if (stdDev == 0) return 100;
        var factor = (tag.ProductCount - mean) / stdDev;
        return factor > 2 ? 150 : factor > 1 ? 120 : factor > 0.5 ? 100 :
            factor > -0.5 ? 90 : factor > -1 ? 85 : factor > -2 ? 80 : 75;
    }
}
