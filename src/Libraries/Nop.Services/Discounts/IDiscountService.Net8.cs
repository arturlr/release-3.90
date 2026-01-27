using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Discounts;

namespace Nop.Services.Discounts
{
    public interface IDiscountService
    {
        Task<Discount> GetDiscountByIdAsync(int discountId);
        Task<IList<Discount>> GetAllDiscountsAsync();
        Task InsertDiscountAsync(Discount discount);
        Task UpdateDiscountAsync(Discount discount);
        Task DeleteDiscountAsync(Discount discount);
        Task<bool> ValidateDiscountAsync(Discount discount, int customerId);
    }
}
