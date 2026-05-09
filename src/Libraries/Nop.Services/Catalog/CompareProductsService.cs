using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Microsoft.AspNetCore.Http;


namespace Nop.Services.Catalog
{
    /// <summary>
    /// Compare products service
    /// </summary>
    public partial class CompareProductsService : ICompareProductsService
    {
        #region Constants

        /// <summary>
        /// Compare products cookie name
        /// </summary>
        private const string COMPARE_PRODUCTS_COOKIE_NAME = "nop.CompareProducts";

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
        public CompareProductsService(IHttpContextAccessor httpContextAccessor, IProductService productService,
            CatalogSettings catalogSettings)
        {
            this._httpContextAccessor = httpContextAccessor;
            this._productService = productService;
            this._catalogSettings = catalogSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets a "compare products" identifier list
        /// </summary>
        /// <returns>"compare products" identifier list</returns>
        protected virtual List<int> GetComparedProductIds()
        {
            var productIds = new List<int>();
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return productIds;

            var cookieValue = httpContext.Request.Cookies[COMPARE_PRODUCTS_COOKIE_NAME];
            if (string.IsNullOrEmpty(cookieValue))
                return productIds;

            var values = cookieValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (string productId in values)
            {
                if (int.TryParse(productId, out int prodId) && !productIds.Contains(prodId))
                    productIds.Add(prodId);
            }

            return productIds;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clears a "compare products" list
        /// </summary>
        public virtual void ClearCompareProducts()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            httpContext.Response.Cookies.Delete(COMPARE_PRODUCTS_COOKIE_NAME);
        }

        /// <summary>
        /// Gets a "compare products" list
        /// </summary>
        /// <returns>"Compare products" list</returns>
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

        /// <summary>
        /// Removes a product from a "compare products" list
        /// </summary>
        /// <param name="productId">Product identifier</param>
        public virtual void RemoveProductFromCompareList(int productId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var oldProductIds = GetComparedProductIds();
            var newProductIds = new List<int>(oldProductIds);
            newProductIds.Remove(productId);

            var cookieValue = string.Join(",", newProductIds);
            httpContext.Response.Cookies.Append(COMPARE_PRODUCTS_COOKIE_NAME, cookieValue, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10.0)
            });
        }

        /// <summary>
        /// Adds a product to a "compare products" list
        /// </summary>
        /// <param name="productId">Product identifier</param>
        public virtual void AddProductToCompareList(int productId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var oldProductIds = GetComparedProductIds();
            var newProductIds = new List<int>();
            newProductIds.Add(productId);
            foreach (int oldProductId in oldProductIds)
                if (oldProductId != productId)
                    newProductIds.Add(oldProductId);

            var idsToStore = newProductIds.Take(_catalogSettings.CompareProductsNumber > 0 ? _catalogSettings.CompareProductsNumber : 4);
            var cookieValue = string.Join(",", idsToStore);

            httpContext.Response.Cookies.Append(COMPARE_PRODUCTS_COOKIE_NAME, cookieValue, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10.0)
            });
        }

        #endregion
    }
}
