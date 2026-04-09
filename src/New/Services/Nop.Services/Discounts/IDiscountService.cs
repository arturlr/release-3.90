using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;

namespace Nop.Services.Discounts;

public interface IDiscountService
{
    // CRUD
    Task<Discount?> GetDiscountByIdAsync(int discountId);
    Task<IList<Discount>> GetAllDiscountsAsync(DiscountType? discountType = null,
        string? couponCode = null, string? discountName = null, bool showHidden = false);
    Task InsertDiscountAsync(Discount discount);
    Task UpdateDiscountAsync(Discount discount);
    Task DeleteDiscountAsync(Discount discount);

    // Entity mappings
    Task<IList<int>> GetAppliedCategoryIdsAsync(int discountId, Customer customer);
    Task<IList<int>> GetAppliedManufacturerIdsAsync(int discountId);
    Task<IList<int>> GetAppliedProductIdsAsync(int discountId);

    // Requirements
    Task<IList<DiscountRequirement>> GetAllDiscountRequirementsAsync(int discountId = 0, bool topLevelOnly = false);
    Task DeleteDiscountRequirementAsync(DiscountRequirement discountRequirement);

    // Validation
    Task<DiscountValidationResult> ValidateDiscountAsync(Discount discount, Customer customer, string[]? couponCodesToValidate = null);

    // Usage history
    Task<DiscountUsageHistory?> GetDiscountUsageHistoryByIdAsync(int discountUsageHistoryId);
    Task<IPagedList<DiscountUsageHistory>> GetAllDiscountUsageHistoryAsync(int? discountId = null,
        int? customerId = null, int? orderId = null, int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertDiscountUsageHistoryAsync(DiscountUsageHistory discountUsageHistory);
    Task UpdateDiscountUsageHistoryAsync(DiscountUsageHistory discountUsageHistory);
    Task DeleteDiscountUsageHistoryAsync(DiscountUsageHistory discountUsageHistory);
}
