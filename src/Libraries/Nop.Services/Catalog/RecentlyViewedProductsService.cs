using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Recently viewed products service
    /// </summary>
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        #region Fields

        private const string RECENTLY_VIEWED_COOKIE_NAME = "NopCommerce.RecentlyViewedProducts";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public RecentlyViewedProductsService(IHttpContextAccessor httpContextAccessor,
            IProductService productService,
            CatalogSettings catalogSettings)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Utilities

        protected IList<int> GetRecentlyViewedProductsIds()
        {
            return GetRecentlyViewedProductsIds(int.MaxValue);
        }

        protected IList<int> GetRecentlyViewedProductsIds(int number)
        {
            var productIds = new List<int>();
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return productIds;

            if (!httpContext.Request.Cookies.TryGetValue(RECENTLY_VIEWED_COOKIE_NAME, out var productIdsStr))
                return productIds;

            if (string.IsNullOrEmpty(productIdsStr))
                return productIds;

            foreach (var id in productIdsStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(id.Trim(), out var prodId) && !productIds.Contains(prodId))
                {
                    productIds.Add(prodId);
                    if (productIds.Count >= number)
                        break;
                }
            }

            return productIds;
        }

        #endregion

        #region Methods

        public virtual IList<Product> GetRecentlyViewedProducts(int number)
        {
            var products = new List<Product>();
            var productIds = GetRecentlyViewedProductsIds(number);
            foreach (var product in _productService.GetProductsByIds(productIds.ToArray()))
                if (product.Published && !product.Deleted)
                    products.Add(product);
            return products;
        }

        public virtual void AddProductToRecentlyViewedList(int productId)
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled)
                return;

            var oldProductIds = GetRecentlyViewedProductsIds();
            var newProductIds = new List<int> { productId };
            foreach (int oldProductId in oldProductIds)
                if (oldProductId != productId)
                    newProductIds.Add(oldProductId);

            int maxProducts = _catalogSettings.RecentlyViewedProductsNumber;
            if (maxProducts <= 0)
                maxProducts = 10;

            var idsToStore = newProductIds.Take(maxProducts).ToList();
            var cookieValue = string.Join(",", idsToStore);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10)
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append(RECENTLY_VIEWED_COOKIE_NAME, cookieValue, cookieOptions);
        }

        #endregion
    }
}
