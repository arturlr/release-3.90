using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    public partial class CompareProductsService : ICompareProductsService
    {
        private const string COMPARE_PRODUCTS_COOKIE_NAME = "nop.CompareProducts";
        
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;
        private readonly CatalogSettings _catalogSettings;

        public CompareProductsService(IHttpContextAccessor httpContextAccessor, 
            IProductService productService,
            CatalogSettings catalogSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
            _catalogSettings = catalogSettings;
        }

        protected virtual List<int> GetComparedProductIds()
        {
            var productIds = new List<int>();
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return productIds;

            if (httpContext.Request.Cookies.TryGetValue(COMPARE_PRODUCTS_COOKIE_NAME, out var productIdsStr) 
                && !string.IsNullOrEmpty(productIdsStr))
            {
                foreach (var id in productIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(id.Trim(), out var prodId) && !productIds.Contains(prodId))
                        productIds.Add(prodId);
                }
            }
            return productIds;
        }

        public virtual void ClearCompareProducts()
        {
            _httpContextAccessor.HttpContext?.Response.Cookies.Delete(COMPARE_PRODUCTS_COOKIE_NAME);
        }

        public virtual IList<Product> GetComparedProducts()
        {
            var products = new List<Product>();
            var productIds = GetComparedProductIds();
            foreach (int productId in productIds)
            {
                var product = _productService.GetProductById(productId);
                if (product != null && product.Published && !product.Deleted)
                    products.Add(product);
            }
            return products;
        }

        public virtual void RemoveProductFromCompareList(int productId)
        {
            var productIds = GetComparedProductIds();
            productIds.Remove(productId);
            SetCompareProductsCookie(productIds);
        }

        public virtual void AddProductToCompareList(int productId)
        {
            var oldProductIds = GetComparedProductIds();
            var newProductIds = new List<int> { productId };
            foreach (int oldProductId in oldProductIds)
            {
                if (oldProductId != productId)
                    newProductIds.Add(oldProductId);
                if (newProductIds.Count >= _catalogSettings.CompareProductsNumber)
                    break;
            }
            SetCompareProductsCookie(newProductIds);
        }

        private void SetCompareProductsCookie(List<int> productIds)
        {
            var cookieValue = string.Join(",", productIds);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10)
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(COMPARE_PRODUCTS_COOKIE_NAME, cookieValue, cookieOptions);
        }
    }
}
