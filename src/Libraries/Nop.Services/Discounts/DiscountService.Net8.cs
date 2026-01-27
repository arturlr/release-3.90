using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Discounts;
using Nop.Data;

namespace Nop.Services.Discounts
{
    public class DiscountService : IDiscountService
    {
        private readonly IRepository<Discount> _discountRepository;

        public DiscountService(IRepository<Discount> discountRepository)
        {
            _discountRepository = discountRepository;
        }

        public virtual async Task<Discount> GetDiscountByIdAsync(int discountId)
        {
            if (discountId == 0)
                return null;

            return await _discountRepository.GetByIdAsync(discountId);
        }

        public virtual async Task<IList<Discount>> GetAllDiscountsAsync()
        {
            var query = _discountRepository.Table.OrderBy(d => d.Name);
            return await query.ToListAsync();
        }

        public virtual async Task InsertDiscountAsync(Discount discount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            await _discountRepository.InsertAsync(discount);
        }

        public virtual async Task UpdateDiscountAsync(Discount discount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            await _discountRepository.UpdateAsync(discount);
        }

        public virtual async Task DeleteDiscountAsync(Discount discount)
        {
            if (discount == null)
                throw new ArgumentNullException(nameof(discount));

            await _discountRepository.DeleteAsync(discount);
        }

        public virtual async Task<bool> ValidateDiscountAsync(Discount discount, int customerId)
        {
            // TODO: Implement discount validation
            return await Task.FromResult(true);
        }
    }
}
