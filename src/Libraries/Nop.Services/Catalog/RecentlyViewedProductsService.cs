using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Microsoft.AspNetCore.Http;


namespace Nop.Services.Catalog
{
    /// <summary>
    /// Recently viewed products service
    /// </summary>
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        #region Fields

        private const string CookieName = "NopCommerce.RecentlyViewedProducts";
        
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor
        
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <param name="productService">Product service</param>
        /// <param name="catalogSettings">Catalog settings</param>
        public RecentlyViewedProductsService(IHttpContextAccessor httpContextAccessor, IProductService productService,
            CatalogSettings catalogSettings)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._productService = productService;
            this._catalogSettings = catalogSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets a "recently viewed products" identifier list
        /// </summary>
        /// <returns>"recently viewed products" list</returns>
        protected IList<int> GetRecentlyViewedProductsIds()
        {
            return GetRecentlyViewedProductsIds(int.MaxValue);
        }

        /// <summary>
        /// Gets a "recently viewed products" identifier list
        /// </summary>
        /// <param name="number">Number of products to load</param>
        /// <returns>"recently viewed products" list</returns>
        protected IList<int> GetRecentlyViewedProductsIds(int number)
        {
            var productIds = new List<int>();
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return productIds;

            var cookieValue = httpContext.Request.Cookies[CookieName];
            if (string.IsNullOrEmpty(cookieValue))
                return productIds;

            var values = cookieValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string productId in values)
            {
                if (int.TryParse(productId, out int prodId) && !productIds.Contains(prodId))
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


        /// <summary>
        /// Gets a "recently viewed products" list
        /// </summary>
        /// <param name="number">Number of products to load</param>
        /// <returns>"recently viewed products" list</returns>
        public virtual IList<Product> GetRecentlyViewedProducts(int number)
        {
            var products = new List<Product>();
            var productIds = GetRecentlyViewedProductsIds(number);
            foreach (var product in _productService.GetProductsByIds(productIds.ToArray()))
                if (product.Published && !product.Deleted)
                    products.Add(product);
            return products;
        }

        /// <summary>
        /// Adds a product to a recently viewed products list
        /// </summary>
        /// <param name="productId">Product identifier</param>
        public virtual void AddProductToRecentlyViewedList(int productId)
        {
            if (!_catalogSettings.RecentlyViewedProductsEnabled)
                return;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var oldProductIds = GetRecentlyViewedProductsIds();
            var newProductIds = new List<int>();
            newProductIds.Add(productId);
            foreach (int oldProductId in oldProductIds)
                if (oldProductId != productId)
                    newProductIds.Add(oldProductId);

            int maxProducts = _catalogSettings.RecentlyViewedProductsNumber;
            if (maxProducts <= 0)
                maxProducts = 10;

            var idsToStore = newProductIds.Take(maxProducts);
            var cookieValue = string.Join(",", idsToStore);

            httpContext.Response.Cookies.Append(CookieName, cookieValue, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10.0)
            });
        }
        
        #endregion
    }
}
