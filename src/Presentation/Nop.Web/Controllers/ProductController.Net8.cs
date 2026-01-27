using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class ProductController : BasePublicController
    {
        private readonly IProductService _productService;

        public ProductController(
            IWorkContext workContext,
            IProductService productService) : base(workContext)
        {
            _productService = productService;
        }

        // GET: /Product/ProductDetails/5
        public async Task<IActionResult> ProductDetails(int productId)
        {
            if (productId <= 0)
                return NotFound();

            var product = await _productService.GetProductByIdAsync(productId);
            if (product == null || !product.Published)
                return NotFound();

            return View(product);
        }

        // GET: /Product/Search
        public async Task<IActionResult> Search(string q, int page = 0)
        {
            var products = await _productService.SearchProductsAsync(q ?? "", page, 10);
            
            var result = $"Search Results for '{q}': {products.TotalCount} products found\n\n";
            foreach (var product in products)
            {
                result += $"- {product.Name} (${product.Price})\n";
            }

            return Content(result);
        }

        // GET: /Category/5
        public async Task<IActionResult> Category(int categoryId, int page = 0)
        {
            if (categoryId <= 0)
                return NotFound();

            var products = await _productService.GetProductsByCategoryAsync(categoryId, page, 10);
            return Content($"Category {categoryId}: {products.TotalCount} products");
        }

        // GET: /Manufacturer/5
        public IActionResult Manufacturer(int manufacturerId)
        {
            if (manufacturerId <= 0)
                return NotFound();

            return Content($"Manufacturer - ID: {manufacturerId}");
        }
    }
}
