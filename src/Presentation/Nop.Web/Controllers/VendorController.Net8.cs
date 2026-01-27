using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class VendorController : BasePublicController
    {
        private readonly IVendorService _vendorService;
        private readonly IProductService _productService;

        public VendorController(
            IWorkContext workContext,
            IVendorService vendorService,
            IProductService productService) : base(workContext)
        {
            _vendorService = vendorService;
            _productService = productService;
        }

        // GET: /Vendor/All
        public async Task<IActionResult> All()
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            return View(vendors);
        }

        // GET: /Vendor/Info/5
        public async Task<IActionResult> Info(int vendorId)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
            if (vendor == null || !vendor.Active || vendor.Deleted)
                return NotFound();

            var products = await _productService.SearchProductsAsync("", 0, 12);
            return View((vendor, products));
        }
    }
}
