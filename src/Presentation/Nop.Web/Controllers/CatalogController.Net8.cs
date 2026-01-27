using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class CatalogController : BasePublicController
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;

        public CatalogController(
            IWorkContext workContext,
            IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService) : base(workContext)
        {
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
        }

        // GET: /Catalog
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        // GET: /Category/5
        public async Task<IActionResult> Category(int categoryId, int page = 0)
        {
            var category = await _categoryService.GetCategoryByIdAsync(categoryId);
            if (category == null)
                return NotFound();

            var products = await _productService.SearchProductsAsync("", page, 12);
            return View((category, products));
        }

        // GET: /Manufacturer/5
        public async Task<IActionResult> Manufacturer(int manufacturerId, int page = 0)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(manufacturerId);
            if (manufacturer == null)
                return NotFound();

            var products = await _productService.SearchProductsAsync("", page, 12);
            var productList = string.Join("\n", products.Select(p => $"{p.Name} - ${p.Price}"));
            return Content($"Manufacturer: {manufacturer.Name}\n\nProducts:\n{productList}");
        }

        // GET: /Search
        public async Task<IActionResult> Search(string q, int page = 0)
        {
            var products = await _productService.SearchProductsAsync(q ?? "", page, 12);
            var productList = string.Join("\n", products.Select(p => $"{p.Name} - ${p.Price}"));
            return Content($"Search Results for '{q}':\n{productList}");
        }

        // GET: /NewProducts
        public async Task<IActionResult> NewProducts()
        {
            var products = await _productService.SearchProductsAsync("", 0, 12);
            var productList = string.Join("\n", products.Select(p => $"{p.Name} - ${p.Price}"));
            return Content($"New Products:\n{productList}");
        }
    }
}
