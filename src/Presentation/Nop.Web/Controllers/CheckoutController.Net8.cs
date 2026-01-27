using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class CheckoutController : BasePublicController
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderService _orderService;

        public CheckoutController(
            IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            IOrderService orderService) : base(workContext)
        {
            _shoppingCartService = shoppingCartService;
            _orderService = orderService;
        }

        // GET: /Checkout
        public async Task<IActionResult> Index()
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(CurrentCustomer);
            if (cart.Count == 0)
                return RedirectToAction("Cart", "ShoppingCart");

            return View();
        }

        // GET: /Checkout/BillingAddress
        public IActionResult BillingAddress()
        {
            return View();
        }

        // POST: /Checkout/BillingAddress
        [HttpPost]
        public IActionResult BillingAddress(int addressId)
        {
            // TODO: Save billing address
            return RedirectToAction("ShippingAddress");
        }

        // GET: /Checkout/ShippingAddress
        public IActionResult ShippingAddress()
        {
            return View();
        }

        // GET: /Checkout/ShippingMethod
        public IActionResult ShippingMethod()
        {
            return View();
        }

        // GET: /Checkout/PaymentMethod
        public IActionResult PaymentMethod()
        {
            return View();
        }

        // GET: /Checkout/Confirm
        public IActionResult Confirm()
        {
            return View();
        }

        // GET: /Checkout/Completed
        public IActionResult Completed()
        {
            return View();
        }
    }
}
