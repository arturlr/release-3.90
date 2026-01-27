using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;

namespace Nop.Services.Orders
{
    public interface IOrderService
    {
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<IList<Order>> GetOrdersByCustomerIdAsync(int customerId);
        Task InsertOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(Order order);
    }
}
