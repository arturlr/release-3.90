using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;
using Nop.Data;
using Microsoft.EntityFrameworkCore;

namespace Nop.Services.Orders
{
    /// <summary>
    /// Shopping cart service implementation
    /// </summary>
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly IRepository<ShoppingCartItem> _shoppingCartRepository;
        private readonly IWorkContext _workContext;

        public ShoppingCartService(
            IRepository<ShoppingCartItem> shoppingCartRepository,
            IWorkContext workContext)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _workContext = workContext;
        }

        /// <summary>
        /// Gets shopping cart
        /// </summary>
        public virtual async Task<IList<ShoppingCartItem>> GetShoppingCartAsync(
            Customer? customer = null,
            ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart)
        {
            customer ??= _workContext.CurrentCustomer;

            return await _shoppingCartRepository.Table
                .Where(sci => sci.CustomerId == customer.Id && 
                             sci.ShoppingCartTypeId == (int)shoppingCartType)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a product to shopping cart
        /// </summary>
        public virtual async Task<ShoppingCartItem> AddToCartAsync(
            Customer customer,
            int productId,
            int quantity = 1,
            ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart)
        {
            // Check if item already exists
            var existingItem = await _shoppingCartRepository.Table
                .FirstOrDefaultAsync(sci => 
                    sci.CustomerId == customer.Id &&
                    sci.ProductId == productId &&
                    sci.ShoppingCartTypeId == (int)shoppingCartType);

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += quantity;
                existingItem.UpdatedOnUtc = DateTime.UtcNow;
                await _shoppingCartRepository.UpdateAsync(existingItem);
                return existingItem;
            }

            // Create new item
            var cartItem = new ShoppingCartItem
            {
                CustomerId = customer.Id,
                ProductId = productId,
                Quantity = quantity,
                ShoppingCartTypeId = (int)shoppingCartType,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };

            await _shoppingCartRepository.InsertAsync(cartItem);
            return cartItem;
        }

        /// <summary>
        /// Updates shopping cart item
        /// </summary>
        public virtual async Task UpdateCartItemAsync(ShoppingCartItem cartItem)
        {
            cartItem.UpdatedOnUtc = DateTime.UtcNow;
            await _shoppingCartRepository.UpdateAsync(cartItem);
        }

        /// <summary>
        /// Deletes shopping cart item
        /// </summary>
        public virtual async Task DeleteCartItemAsync(ShoppingCartItem cartItem)
        {
            await _shoppingCartRepository.DeleteAsync(cartItem);
        }

        /// <summary>
        /// Clears shopping cart
        /// </summary>
        public virtual async Task ClearCartAsync(Customer customer)
        {
            var cartItems = await GetShoppingCartAsync(customer);
            foreach (var item in cartItems)
            {
                await _shoppingCartRepository.DeleteAsync(item);
            }
        }
    }

    /// <summary>
    /// Shopping cart type enum
    /// </summary>
    public enum ShoppingCartType
    {
        ShoppingCart = 1,
        Wishlist = 2
    }
}
