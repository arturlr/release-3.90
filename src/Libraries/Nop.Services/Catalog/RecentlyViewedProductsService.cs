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
    /// <remarks>
    /// Task 4.2 (design section 5): re-based from <c>System.Web.HttpContextBase</c> onto
    /// <see cref="IHttpContextAccessor"/>. As in
    /// <see cref="CompareProductsService"/>, <c>System.Web.HttpCookie.Values</c>
    /// (a multi-valued sub-key collection) has no ASP.NET Core counterpart, so the ids are
    /// stored as one comma-separated cookie value under the same cookie name.
    /// </remarks>
    public partial class RecentlyViewedProductsService : IRecentlyViewedProductsService
    {
        #region Constants

        /// <summary>
        /// Recently viewed products cookie name
        /// </summary>
        private const string RECENTLY_VIEWED_PRODUCTS_COOKIE_NAME = "NopCommerce.RecentlyViewedProducts";

        #endregion

        #region Fields

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
        /// Gets the current HTTP context, or null when there is no current request
        /// </summary>
        protected virtual HttpContext HttpContext
        {
            get { return _httpContextAccessor == null ? null : _httpContextAccessor.HttpContext; }
        }

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

            var httpContext = HttpContext;
            if (httpContext == null)
                return productIds;

            string cookieValue;
            if (!httpContext.Request.Cookies.TryGetValue(RECENTLY_VIEWED_PRODUCTS_COOKIE_NAME, out cookieValue))
                return productIds;
            if (String.IsNullOrEmpty(cookieValue))
                return productIds;

            foreach (var productId in cookieValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int prodId;
                if (!int.TryParse(productId.Trim(), out prodId))
                    continue;
                if (!productIds.Contains(prodId))
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

            var httpContext = HttpContext;
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

            //identical truncation loop to 3.90
            var persistedProductIds = new List<int>();
            int i = 1;
            foreach (int newProductId in newProductIds)
            {
                persistedProductIds.Add(newProductId);
                if (i == maxProducts)
                    break;
                i++;
            }

            var options = new CookieOptions
            {
                //preserved from the legacy cookie: HttpOnly, plus the 10-day sliding lifetime
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10.0)
            };
            httpContext.Response.Cookies.Append(RECENTLY_VIEWED_PRODUCTS_COOKIE_NAME,
                String.Join(",", persistedProductIds.Select(id => id.ToString())), options);
        }
        
        #endregion
    }
}
