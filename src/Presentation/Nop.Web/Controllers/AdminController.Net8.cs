using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.News;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    [Route("Admin")]
    public class AdminController : BaseController
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly INewsService _newsService;

        public AdminController(
            IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ICustomerService customerService,
            IOrderService orderService,
            INewsService newsService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _customerService = customerService;
            _orderService = orderService;
            _newsService = newsService;
        }

        // GET: /Admin
        public IActionResult Index()
        {
            return View();
        }

        #region Products

        // GET: /Admin/Product
        [Route("Product")]
        public async Task<IActionResult> ProductList()
        {
            var products = await _productService.SearchProductsAsync("", 0, 100);
            return View(products);
        }

        // GET: /Admin/Product/Create
        [Route("Product/Create")]
        public IActionResult ProductCreate()
        {
            return View("ProductEdit", new Core.Domain.Catalog.Product());
        }

        // POST: /Admin/Product/Create
        [HttpPost]
        [Route("Product/Create")]
        public async Task<IActionResult> ProductCreate(string name, string shortDescription, string fullDescription, 
            decimal price, decimal oldPrice, string sku, int stockQuantity, bool published)
        {
            var product = new Core.Domain.Catalog.Product
            {
                Name = name,
                ShortDescription = shortDescription,
                FullDescription = fullDescription,
                Price = price,
                OldPrice = oldPrice,
                Sku = sku,
                StockQuantity = stockQuantity,
                Published = published,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _productService.InsertProductAsync(product);
            TempData["SuccessMessage"] = "Product created successfully!";
            return RedirectToAction("ProductList");
        }

        // GET: /Admin/Product/Edit/5
        [Route("Product/Edit/{id}")]
        public async Task<IActionResult> ProductEdit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // POST: /Admin/Product/Edit
        [HttpPost]
        [Route("Product/Edit")]
        public async Task<IActionResult> ProductEdit(int id, string name, string shortDescription, string fullDescription,
            decimal price, decimal oldPrice, string sku, int stockQuantity, bool published)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            product.Name = name;
            product.ShortDescription = shortDescription;
            product.FullDescription = fullDescription;
            product.Price = price;
            product.OldPrice = oldPrice;
            product.Sku = sku;
            product.StockQuantity = stockQuantity;
            product.Published = published;
            product.UpdatedOnUtc = DateTime.UtcNow;

            await _productService.UpdateProductAsync(product);
            TempData["SuccessMessage"] = "Product updated successfully!";
            return RedirectToAction("ProductList");
        }

        // POST: /Admin/Product/Delete/5
        [HttpPost]
        [Route("Product/Delete/{id}")]
        public async Task<IActionResult> ProductDelete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            await _productService.DeleteProductAsync(product);
            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction("ProductList");
        }

        #endregion

        #region Categories

        // GET: /Admin/Category
        [Route("Category")]
        public async Task<IActionResult> CategoryList()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        // GET: /Admin/Category/Create
        [Route("Category/Create")]
        public IActionResult CategoryCreate()
        {
            return View("CategoryEdit", new Core.Domain.Catalog.Category());
        }

        // POST: /Admin/Category/Create
        [HttpPost]
        [Route("Category/Create")]
        public async Task<IActionResult> CategoryCreate(string name, string description, int displayOrder, int parentCategoryId, bool published)
        {
            var category = new Core.Domain.Catalog.Category
            {
                Name = name,
                Description = description,
                DisplayOrder = displayOrder,
                ParentCategoryId = parentCategoryId,
                Published = published,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _categoryService.InsertCategoryAsync(category);
            TempData["SuccessMessage"] = "Category created successfully!";
            return RedirectToAction("CategoryList");
        }

        // GET: /Admin/Category/Edit/5
        [Route("Category/Edit/{id}")]
        public async Task<IActionResult> CategoryEdit(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: /Admin/Category/Edit
        [HttpPost]
        [Route("Category/Edit")]
        public async Task<IActionResult> CategoryEdit(int id, string name, string description, int displayOrder, int parentCategoryId, bool published)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            category.Name = name;
            category.Description = description;
            category.DisplayOrder = displayOrder;
            category.ParentCategoryId = parentCategoryId;
            category.Published = published;
            category.UpdatedOnUtc = DateTime.UtcNow;

            await _categoryService.UpdateCategoryAsync(category);
            TempData["SuccessMessage"] = "Category updated successfully!";
            return RedirectToAction("CategoryList");
        }

        // POST: /Admin/Category/Delete/5
        [HttpPost]
        [Route("Category/Delete/{id}")]
        public async Task<IActionResult> CategoryDelete(int id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound();

            await _categoryService.DeleteCategoryAsync(category);
            TempData["SuccessMessage"] = "Category deleted successfully!";
            return RedirectToAction("CategoryList");
        }

        #endregion

        #region Orders

        // GET: /Admin/Order
        [Route("Order")]
        public async Task<IActionResult> OrderList()
        {
            // TODO: Get all orders
            var orders = new List<Core.Domain.Orders.Order>();
            return View(orders);
        }

        // GET: /Admin/Order/Edit/5
        [Route("Order/Edit/{id}")]
        public async Task<IActionResult> OrderEdit(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return View(order);
        }

        #endregion

        #region Customers

        // GET: /Admin/Customer
        [Route("Customer")]
        public async Task<IActionResult> CustomerList()
        {
            // TODO: Get all customers
            var customers = new List<Core.Domain.Customers.Customer>();
            return View(customers);
        }

        // GET: /Admin/Customer/Edit/5
        [Route("Customer/Edit/{id}")]
        public async Task<IActionResult> CustomerEdit(int id)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();

            return View(customer);
        }

        // POST: /Admin/Customer/Edit
        [HttpPost]
        [Route("Customer/Edit")]
        public async Task<IActionResult> CustomerEdit(int id, string email, string username, bool active)
        {
            var customer = await _customerService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();

            customer.Email = email;
            customer.Username = username;
            customer.Active = active;

            await _customerService.UpdateCustomerAsync(customer);
            TempData["SuccessMessage"] = "Customer updated successfully!";
            return RedirectToAction("CustomerList");
        }

        #endregion

        #region Manufacturers

        [Route("Manufacturer")]
        public async Task<IActionResult> ManufacturerList()
        {
            var manufacturers = await _manufacturerService.GetAllManufacturersAsync();
            return View(manufacturers);
        }

        [Route("Manufacturer/Create")]
        public IActionResult ManufacturerCreate()
        {
            return View("ManufacturerEdit", new Core.Domain.Catalog.Manufacturer());
        }

        [HttpPost]
        [Route("Manufacturer/Create")]
        public async Task<IActionResult> ManufacturerCreate(string name, string description, int displayOrder, bool published)
        {
            var manufacturer = new Core.Domain.Catalog.Manufacturer
            {
                Name = name,
                Description = description,
                DisplayOrder = displayOrder,
                Published = published,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _manufacturerService.InsertManufacturerAsync(manufacturer);
            TempData["SuccessMessage"] = "Manufacturer created successfully!";
            return RedirectToAction("ManufacturerList");
        }

        [Route("Manufacturer/Edit/{id}")]
        public async Task<IActionResult> ManufacturerEdit(int id)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(id);
            if (manufacturer == null)
                return NotFound();

            return View(manufacturer);
        }

        [HttpPost]
        [Route("Manufacturer/Edit")]
        public async Task<IActionResult> ManufacturerEdit(int id, string name, string description, int displayOrder, bool published)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(id);
            if (manufacturer == null)
                return NotFound();

            manufacturer.Name = name;
            manufacturer.Description = description;
            manufacturer.DisplayOrder = displayOrder;
            manufacturer.Published = published;
            manufacturer.UpdatedOnUtc = DateTime.UtcNow;

            await _manufacturerService.UpdateManufacturerAsync(manufacturer);
            TempData["SuccessMessage"] = "Manufacturer updated successfully!";
            return RedirectToAction("ManufacturerList");
        }

        [HttpPost]
        [Route("Manufacturer/Delete/{id}")]
        public async Task<IActionResult> ManufacturerDelete(int id)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(id);
            if (manufacturer == null)
                return NotFound();

            await _manufacturerService.DeleteManufacturerAsync(manufacturer);
            TempData["SuccessMessage"] = "Manufacturer deleted successfully!";
            return RedirectToAction("ManufacturerList");
        }

        #endregion

        #region News

        [Route("News")]
        public async Task<IActionResult> NewsList()
        {
            var news = await _newsService.GetAllNewsAsync(0, 100);
            return View(news);
        }

        [Route("News/Create")]
        public IActionResult NewsCreate()
        {
            return View("NewsEdit", new Core.Domain.News.NewsItem());
        }

        [HttpPost]
        [Route("News/Create")]
        public async Task<IActionResult> NewsCreate(string title, string shortText, string full, bool published, bool allowComments)
        {
            var news = new Core.Domain.News.NewsItem
            {
                Title = title,
                Short = shortText,
                Full = full,
                Published = published,
                AllowComments = allowComments,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _newsService.InsertNewsAsync(news);
            TempData["SuccessMessage"] = "News created successfully!";
            return RedirectToAction("NewsList");
        }

        [Route("News/Edit/{id}")]
        public async Task<IActionResult> NewsEdit(int id)
        {
            var news = await _newsService.GetNewsByIdAsync(id);
            if (news == null)
                return NotFound();

            return View(news);
        }

        [HttpPost]
        [Route("News/Edit")]
        public async Task<IActionResult> NewsEdit(int id, string title, string shortText, string full, bool published, bool allowComments)
        {
            var news = await _newsService.GetNewsByIdAsync(id);
            if (news == null)
                return NotFound();

            news.Title = title;
            news.Short = shortText;
            news.Full = full;
            news.Published = published;
            news.AllowComments = allowComments;

            await _newsService.UpdateNewsAsync(news);
            TempData["SuccessMessage"] = "News updated successfully!";
            return RedirectToAction("NewsList");
        }

        [HttpPost]
        [Route("News/Delete/{id}")]
        public async Task<IActionResult> NewsDelete(int id)
        {
            var news = await _newsService.GetNewsByIdAsync(id);
            if (news == null)
                return NotFound();

            await _newsService.DeleteNewsAsync(news);
            TempData["SuccessMessage"] = "News deleted successfully!";
            return RedirectToAction("NewsList");
        }

        #endregion

        #region Reports & Plugins

        [Route("Report")]
        public IActionResult Reports()
        {
            return View();
        }

        [Route("Plugin")]
        public IActionResult Plugins()
        {
            return View();
        }

        #endregion

        #region Settings

        // GET: /Admin/Setting
        [Route("Setting")]
        public IActionResult Settings()
        {
            return View();
        }

        // GET: /Admin/Log
        [Route("Log")]
        public IActionResult SystemLog()
        {
            return View();
        }

        #endregion
    }
}
