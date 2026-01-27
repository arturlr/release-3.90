using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace Nop.Web.Controllers
{
    public class ShoppingCartController : BasePublicController
    {
        private readonly IShoppingCartService _shoppingCartService;

        public ShoppingCartController(
            IWorkContext workContext,
            IShoppingCartService shoppingCartService) : base(workContext)
        {
            _shoppingCartService = shoppingCartService;
        }

        // GET: /ShoppingCart/Cart
        public async Task<IActionResult> Cart()
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(CurrentCustomer);
            return View(cart);
        }

        // POST: /ShoppingCart/AddProductToCart
        [HttpPost]
        public async Task<IActionResult> AddProductToCart(int productId, int quantity = 1)
        {
            if (productId <= 0)
                return BadRequest();

            try
            {
                await _shoppingCartService.AddToCartAsync(CurrentCustomer, productId, quantity);
                return Json(new { success = true, message = "Product added to cart" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /ShoppingCart/UpdateCart
        [HttpPost]
        public async Task<IActionResult> UpdateCart(int cartItemId, int quantity)
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(CurrentCustomer);
            var item = cart.FirstOrDefault(x => x.Id == cartItemId);
            
            if (item == null)
                return Json(new { success = false, message = "Item not found" });

            item.Quantity = quantity;
            await _shoppingCartService.UpdateCartItemAsync(item);
            
            return Json(new { success = true });
        }

        // POST: /ShoppingCart/DeleteCartItem
        [HttpPost]
        public async Task<IActionResult> DeleteCartItem(int cartItemId)
        {
            var cart = await _shoppingCartService.GetShoppingCartAsync(CurrentCustomer);
            var item = cart.FirstOrDefault(x => x.Id == cartItemId);
            
            if (item == null)
                return Json(new { success = false, message = "Item not found" });

            await _shoppingCartService.DeleteCartItemAsync(item);
            return Json(new { success = true });
        }

        // GET: /ShoppingCart/Wishlist
        public async Task<IActionResult> Wishlist()
        {
            var wishlist = await _shoppingCartService.GetShoppingCartAsync(
                CurrentCustomer, 
                ShoppingCartType.Wishlist);
            
            return Content($"Wishlist ({wishlist.Count} items)");
        }
    }
}
