using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public class RecentlyViewedProductsService : IRecentlyViewedProductsService
{
    private const string RecentlyViewedCookieName = ".Nop.RecentlyViewedProducts";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProductService _productService;
    private readonly CatalogSettings _catalogSettings;

    public RecentlyViewedProductsService(
        IHttpContextAccessor httpContextAccessor,
        IProductService productService,
        CatalogSettings catalogSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _productService = productService;
        _catalogSettings = catalogSettings;
    }

    public virtual async Task<IList<Product>> GetRecentlyViewedProductsAsync(int number)
    {
        var productIds = GetRecentlyViewedProductIds(number);
        var products = new List<Product>();
        foreach (var id in productIds)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null && !product.Deleted && product.Published)
                products.Add(product);
        }
        return products;
    }

    public virtual void AddProductToRecentlyViewedList(int productId)
    {
        if (!_catalogSettings.RecentlyViewedProductsEnabled) return;

        var productIds = GetRecentlyViewedProductIds(_catalogSettings.RecentlyViewedProductsNumber);
        productIds.Remove(productId);
        productIds.Insert(0, productId);

        while (productIds.Count > _catalogSettings.RecentlyViewedProductsNumber)
            productIds.RemoveAt(productIds.Count - 1);

        SetRecentlyViewedProductIds(productIds);
    }

    private List<int> GetRecentlyViewedProductIds(int number)
    {
        var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[RecentlyViewedCookieName];
        if (string.IsNullOrEmpty(cookieValue)) return [];

        return cookieValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0).Take(number).ToList();
    }

    private void SetRecentlyViewedProductIds(List<int> productIds)
    {
        var cookieValue = string.Join(",", productIds);
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(RecentlyViewedCookieName, cookieValue,
            new CookieOptions { HttpOnly = true, Expires = DateTime.UtcNow.AddDays(10) });
    }
}
