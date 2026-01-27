using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Orders
{
    /// <summary>
    /// Shopping cart service interface
    /// </summary>
    public interface IShoppingCartService
    {
        Task<IList<ShoppingCartItem>> GetShoppingCartAsync(Customer? customer = null, ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart);
        Task<ShoppingCartItem> AddToCartAsync(Customer customer, int productId, int quantity = 1, ShoppingCartType shoppingCartType = ShoppingCartType.ShoppingCart);
        Task UpdateCartItemAsync(ShoppingCartItem cartItem);
        Task DeleteCartItemAsync(ShoppingCartItem cartItem);
        Task ClearCartAsync(Customer customer);
    }
}
