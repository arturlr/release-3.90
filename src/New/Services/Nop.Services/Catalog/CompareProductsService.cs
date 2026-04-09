using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public class CompareProductsService : ICompareProductsService
{
    private const string CompareCookieName = ".Nop.CompareProducts";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProductService _productService;
    private readonly CatalogSettings _catalogSettings;

    public CompareProductsService(
        IHttpContextAccessor httpContextAccessor,
        IProductService productService,
        CatalogSettings catalogSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _productService = productService;
        _catalogSettings = catalogSettings;
    }

    public virtual void ClearCompareProducts()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete(CompareCookieName);
    }

    public virtual async Task<IList<Product>> GetComparedProductsAsync()
    {
        var productIds = GetComparedProductIds();
        var products = new List<Product>();
        foreach (var id in productIds)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null && !product.Deleted && product.Published)
                products.Add(product);
        }
        return products;
    }

    public virtual void RemoveProductFromCompareList(int productId)
    {
        var productIds = GetComparedProductIds();
        productIds.Remove(productId);
        SetComparedProductIds(productIds);
    }

    public virtual void AddProductToCompareList(int productId)
    {
        var productIds = GetComparedProductIds();
        if (!productIds.Contains(productId))
        {
            productIds.Insert(0, productId);
            // limit to max compare products
            while (productIds.Count > _catalogSettings.CompareProductsNumber)
                productIds.RemoveAt(productIds.Count - 1);
        }
        SetComparedProductIds(productIds);
    }

    private List<int> GetComparedProductIds()
    {
        var cookieValue = _httpContextAccessor.HttpContext?.Request.Cookies[CompareCookieName];
        if (string.IsNullOrEmpty(cookieValue)) return [];

        return cookieValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0).ToList();
    }

    private void SetComparedProductIds(List<int> productIds)
    {
        var cookieValue = string.Join(",", productIds);
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(CompareCookieName, cookieValue,
            new CookieOptions { HttpOnly = true, Expires = DateTime.UtcNow.AddDays(10) });
    }
}
