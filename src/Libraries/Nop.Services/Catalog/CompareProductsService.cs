using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Compare products service
    /// </summary>
    /// <remarks>
    /// Task 4.2 (design section 5): re-based from <c>System.Web.HttpContextBase</c> onto
    /// <see cref="IHttpContextAccessor"/>.
    ///
    /// The legacy code used <c>System.Web.HttpCookie</c>, whose <c>Values</c> property is a
    /// multi-valued sub-key collection ("a=1&amp;a=2" inside a single cookie). ASP.NET Core
    /// has no such type: <see cref="IRequestCookieCollection"/> and
    /// <see cref="IResponseCookies"/> deal in flat string values. The compared-product ids
    /// are therefore stored as a single comma-separated value under the same cookie name,
    /// which round-trips identically for this service's own reads and writes.
    /// See the note in runtime-deferrals.md - the on-the-wire cookie payload changes, so a
    /// visitor holding a 3.90-era cookie simply starts with an empty compare list.
    /// </remarks>
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
        /// Gets the current HTTP context, or null when there is no current request
        /// </summary>
        protected virtual HttpContext HttpContext
        {
            get { return _httpContextAccessor == null ? null : _httpContextAccessor.HttpContext; }
        }

        /// <summary>
        /// Gets a "compare products" identifier list
        /// </summary>
        /// <returns>"compare products" identifier list</returns>
        protected virtual List<int> GetComparedProductIds()
        {
            var productIds = new List<int>();

            var httpContext = HttpContext;
            if (httpContext == null)
                return productIds;

            string cookieValue;
            if (!httpContext.Request.Cookies.TryGetValue(COMPARE_PRODUCTS_COOKIE_NAME, out cookieValue))
                return productIds;
            if (String.IsNullOrEmpty(cookieValue))
                return productIds;

            foreach (var productId in cookieValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int prodId;
                if (!int.TryParse(productId.Trim(), out prodId))
                    continue;
                if (!productIds.Contains(prodId))
                    productIds.Add(prodId);
            }

            return productIds;
        }

        /// <summary>
        /// Writes the "compare products" cookie
        /// </summary>
        /// <param name="productIds">Product identifiers to persist</param>
        protected virtual void SetComparedProductIds(IEnumerable<int> productIds)
        {
            var httpContext = HttpContext;
            if (httpContext == null)
                return;

            var options = new CookieOptions
            {
                //preserved from the legacy cookie: HttpOnly, plus the 10-day sliding lifetime
                HttpOnly = true,
                Expires = DateTime.Now.AddDays(10.0)
            };
            httpContext.Response.Cookies.Append(COMPARE_PRODUCTS_COOKIE_NAME,
                String.Join(",", productIds.Select(id => id.ToString())), options);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Clears a "compare products" list
        /// </summary>
        public virtual void ClearCompareProducts()
        {
            var httpContext = HttpContext;
            if (httpContext == null)
                return;

            if (!httpContext.Request.Cookies.ContainsKey(COMPARE_PRODUCTS_COOKIE_NAME))
                return;

            //legacy code emptied the values and back-dated Expires by a year; Delete is the
            //ASP.NET Core equivalent (it emits the same expired Set-Cookie).
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
            var httpContext = HttpContext;
            if (httpContext == null)
                return;

            //legacy code returned without writing anything when no cookie existed
            if (!httpContext.Request.Cookies.ContainsKey(COMPARE_PRODUCTS_COOKIE_NAME))
                return;

            var oldProductIds = GetComparedProductIds();
            var newProductIds = new List<int>();
            newProductIds.AddRange(oldProductIds);
            newProductIds.Remove(productId);

            SetComparedProductIds(newProductIds);
        }

        /// <summary>
        /// Adds a product to a "compare products" list
        /// </summary>
        /// <param name="productId">Product identifier</param>
        public virtual void AddProductToCompareList(int productId)
        {
            var oldProductIds = GetComparedProductIds();
            var newProductIds = new List<int>();
            newProductIds.Add(productId);
            foreach (int oldProductId in oldProductIds)
                if (oldProductId != productId)
                    newProductIds.Add(oldProductId);

            //truncate to the configured maximum - identical loop bound to 3.90
            var persistedProductIds = new List<int>();
            int i = 1;
            foreach (int newProductId in newProductIds)
            {
                persistedProductIds.Add(newProductId);
                if (i == _catalogSettings.CompareProductsNumber)
                    break;
                i++;
            }

            SetComparedProductIds(persistedProductIds);
        }

        #endregion
    }
}
