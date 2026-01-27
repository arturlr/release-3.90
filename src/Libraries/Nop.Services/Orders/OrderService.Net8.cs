using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Orders;
using Nop.Data;

namespace Nop.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;

        public OrderService(IRepository<Order> orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public virtual async Task<Order> GetOrderByIdAsync(int orderId)
        {
            if (orderId == 0)
                return null;

            return await _orderRepository.GetByIdAsync(orderId);
        }

        public virtual async Task<IList<Order>> GetOrdersByCustomerIdAsync(int customerId)
        {
            var query = _orderRepository.Table
                .Where(o => o.CustomerId == customerId && !o.Deleted)
                .OrderByDescending(o => o.CreatedOnUtc);

            return await query.ToListAsync();
        }

        public virtual async Task InsertOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            await _orderRepository.InsertAsync(order);
        }

        public virtual async Task UpdateOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            await _orderRepository.UpdateAsync(order);
        }

        public virtual async Task DeleteOrderAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            order.Deleted = true;
            await _orderRepository.UpdateAsync(order);
        }
    }
}
