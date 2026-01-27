using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class OrderController : BasePublicController
    {
        private readonly IOrderService _orderService;

        public OrderController(
            IWorkContext workContext,
            IOrderService orderService) : base(workContext)
        {
            _orderService = orderService;
        }

        // GET: /Order/CustomerOrders
        public async Task<IActionResult> CustomerOrders()
        {
            var orders = await _orderService.GetOrdersByCustomerIdAsync(CurrentCustomer.Id);
            return View(orders);
        }

        // GET: /Order/Details/5
        public async Task<IActionResult> Details(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null || order.CustomerId != CurrentCustomer.Id)
                return NotFound();

            return View(order);
        }

        // GET: /Order/ReOrder/5
        public async Task<IActionResult> ReOrder(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null || order.CustomerId != CurrentCustomer.Id)
                return NotFound();

            // TODO: Add order items to cart
            return RedirectToAction("Cart", "ShoppingCart");
        }
    }
}
