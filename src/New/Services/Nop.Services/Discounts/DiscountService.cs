using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;

namespace Nop.Services.Discounts;

public class DiscountService : IDiscountService
{
    #region Cache keys

    private const string DiscountAllPrefix = "Nop.discount.all.";
    private const string DiscountRequirementPrefix = "Nop.discount.req.";
    private const string DiscountCategoryIdsPrefix = "Nop.discount.catids.";
    private const string DiscountManufacturerIdsPrefix = "Nop.discount.mfrids.";

    #endregion

    #region Fields

    private readonly IRepository<Discount> _discountRepository;
    private readonly IRepository<DiscountRequirement> _discountRequirementRepository;
    private readonly IRepository<DiscountUsageHistory> _discountUsageHistoryRepository;
    private readonly IRepository<DiscountCategoryMapping> _discountCategoryMappingRepository;
    private readonly IRepository<DiscountManufacturerMapping> _discountManufacturerMappingRepository;
    private readonly IRepository<DiscountProductMapping> _discountProductMappingRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IStaticCacheManager _cacheManager;
    private readonly ILocalizationService _localizationService;
    private readonly ICategoryService _categoryService;
    private readonly ICustomerService _customerService;
    private readonly IEventPublisher _eventPublisher;

    #endregion

    #region Ctor

    public DiscountService(
        IRepository<Discount> discountRepository,
        IRepository<DiscountRequirement> discountRequirementRepository,
        IRepository<DiscountUsageHistory> discountUsageHistoryRepository,
        IRepository<DiscountCategoryMapping> discountCategoryMappingRepository,
        IRepository<DiscountManufacturerMapping> discountManufacturerMappingRepository,
        IRepository<DiscountProductMapping> discountProductMappingRepository,
        IRepository<Order> orderRepository,
        IStaticCacheManager cacheManager,
        ILocalizationService localizationService,
        ICategoryService categoryService,
        ICustomerService customerService,
        IEventPublisher eventPublisher)
    {
        _discountRepository = discountRepository;
        _discountRequirementRepository = discountRequirementRepository;
        _discountUsageHistoryRepository = discountUsageHistoryRepository;
        _discountCategoryMappingRepository = discountCategoryMappingRepository;
        _discountManufacturerMappingRepository = discountManufacturerMappingRepository;
        _discountProductMappingRepository = discountProductMappingRepository;
        _orderRepository = orderRepository;
        _cacheManager = cacheManager;
        _localizationService = localizationService;
        _categoryService = categoryService;
        _customerService = customerService;
        _eventPublisher = eventPublisher;
    }

    #endregion

    #region CRUD

    public virtual Task<Discount?> GetDiscountByIdAsync(int discountId)
    {
        if (discountId == 0) return Task.FromResult<Discount?>(null);
        return Task.FromResult(_discountRepository.GetById(discountId));
    }

    public virtual Task<IList<Discount>> GetAllDiscountsAsync(DiscountType? discountType = null,
        string? couponCode = null, string? discountName = null, bool showHidden = false)
    {
        var key = new CacheKey($"{DiscountAllPrefix}{showHidden}-{couponCode ?? ""}-{discountName ?? ""}", DiscountAllPrefix);
        return _cacheManager.GetAsync(key, () =>
        {
            var query = _discountRepository.Table;

            if (!showHidden)
            {
                var nowUtc = DateTime.UtcNow;
                query = query.Where(d =>
                    (!d.StartDateUtc.HasValue || d.StartDateUtc <= nowUtc) &&
                    (!d.EndDateUtc.HasValue || d.EndDateUtc >= nowUtc));
            }

            if (!string.IsNullOrEmpty(couponCode))
                query = query.Where(d => d.CouponCode == couponCode);

            if (!string.IsNullOrEmpty(discountName))
                query = query.Where(d => d.Name != null && d.Name.Contains(discountName));

            query = query.OrderBy(d => d.Name);

            // load all, then filter by type in memory (matching legacy — single cache entry for all types)
            var allDiscounts = query.ToList();

            IList<Discount> result = discountType.HasValue
                ? allDiscounts.Where(d => d.DiscountType == discountType.Value).ToList()
                : allDiscounts;

            return Task.FromResult(result);
        })!;
    }

    public virtual async Task InsertDiscountAsync(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);
        _discountRepository.Insert(discount);
        await _cacheManager.RemoveByPrefixAsync(DiscountAllPrefix);
        await _eventPublisher.EntityInsertedAsync(discount);
    }

    public virtual async Task UpdateDiscountAsync(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);
        _discountRepository.Update(discount);
        await _cacheManager.RemoveByPrefixAsync(DiscountAllPrefix);
        await _eventPublisher.EntityUpdatedAsync(discount);
    }

    public virtual async Task DeleteDiscountAsync(Discount discount)
    {
        ArgumentNullException.ThrowIfNull(discount);
        _discountRepository.Delete(discount);
        await _cacheManager.RemoveByPrefixAsync(DiscountAllPrefix);
        await _eventPublisher.EntityDeletedAsync(discount);
    }

    #endregion

    #region Entity mappings

    public virtual Task<IList<int>> GetAppliedCategoryIdsAsync(int discountId, Customer customer)
    {
        var key = new CacheKey($"{DiscountCategoryIdsPrefix}{discountId}", DiscountCategoryIdsPrefix);
        return _cacheManager.GetAsync(key, async () =>
        {
            var rootCategoryIds = _discountCategoryMappingRepository.TableNoTracking
                .Where(m => m.DiscountId == discountId)
                .Select(m => m.CategoryId)
                .ToList();

            var ids = new List<int>(rootCategoryIds);

            // check if discount applies to subcategories
            var discount = _discountRepository.GetById(discountId);
            if (discount?.AppliedToSubCategories == true)
            {
                foreach (var categoryId in rootCategoryIds)
                {
                    var children = await _categoryService.GetAllCategoriesByParentCategoryIdAsync(categoryId, showHidden: false, includeAllLevels: true);
                    foreach (var child in children)
                    {
                        if (!ids.Contains(child.Id))
                            ids.Add(child.Id);
                    }
                }
            }

            return (IList<int>)ids;
        })!;
    }

    public virtual Task<IList<int>> GetAppliedManufacturerIdsAsync(int discountId)
    {
        var key = new CacheKey($"{DiscountManufacturerIdsPrefix}{discountId}", DiscountManufacturerIdsPrefix);
        return _cacheManager.GetAsync(key, () =>
        {
            IList<int> result = _discountManufacturerMappingRepository.TableNoTracking
                .Where(m => m.DiscountId == discountId)
                .Select(m => m.ManufacturerId)
                .ToList();
            return Task.FromResult(result);
        })!;
    }

    public virtual Task<IList<int>> GetAppliedProductIdsAsync(int discountId)
    {
        IList<int> result = _discountProductMappingRepository.TableNoTracking
            .Where(m => m.DiscountId == discountId)
            .Select(m => m.ProductId)
            .ToList();
        return Task.FromResult(result);
    }

    #endregion

    #region Requirements

    public virtual Task<IList<DiscountRequirement>> GetAllDiscountRequirementsAsync(int discountId = 0, bool topLevelOnly = false)
    {
        var query = _discountRequirementRepository.Table;

        if (discountId > 0)
            query = query.Where(r => r.DiscountId == discountId);

        if (topLevelOnly)
            query = query.Where(r => !r.ParentId.HasValue);

        query = query.OrderBy(r => r.Id);

        IList<DiscountRequirement> result = query.ToList();
        return Task.FromResult(result);
    }

    public virtual async Task DeleteDiscountRequirementAsync(DiscountRequirement discountRequirement)
    {
        ArgumentNullException.ThrowIfNull(discountRequirement);
        _discountRequirementRepository.Delete(discountRequirement);
        await _cacheManager.RemoveByPrefixAsync(DiscountRequirementPrefix);
        await _eventPublisher.EntityDeletedAsync(discountRequirement);
    }

    #endregion

    #region Validation

    public virtual async Task<DiscountValidationResult> ValidateDiscountAsync(Discount discount, Customer customer, string[]? couponCodesToValidate = null)
    {
        ArgumentNullException.ThrowIfNull(discount);
        ArgumentNullException.ThrowIfNull(customer);

        var result = new DiscountValidationResult();

        // coupon code check
        if (discount.RequiresCouponCode)
        {
            if (string.IsNullOrEmpty(discount.CouponCode))
                return result;
            if (couponCodesToValidate == null || !couponCodesToValidate.Any(c => c.Equals(discount.CouponCode, StringComparison.OrdinalIgnoreCase)))
                return result;
        }

        // date range
        var nowUtc = DateTime.UtcNow;
        if (discount.StartDateUtc.HasValue && discount.StartDateUtc.Value > nowUtc)
        {
            result.Errors = [await _localizationService.GetResourceAsync("ShoppingCart.Discount.NotStartedYet")];
            return result;
        }
        if (discount.EndDateUtc.HasValue && discount.EndDateUtc.Value < nowUtc)
        {
            result.Errors = [await _localizationService.GetResourceAsync("ShoppingCart.Discount.Expired")];
            return result;
        }

        // usage limits
        switch (discount.DiscountLimitation)
        {
            case DiscountLimitationType.NTimesOnly:
                {
                    var usedTimes = (await GetAllDiscountUsageHistoryAsync(discount.Id, pageSize: 1)).TotalCount;
                    if (usedTimes >= discount.LimitationTimes)
                        return result;
                    break;
                }
            case DiscountLimitationType.NTimesPerCustomer:
                {
                    var roleIds = await _customerService.GetCustomerRoleIdsAsync(customer);
                    var registeredRole = await _customerService.GetCustomerRoleBySystemNameAsync(SystemCustomerRoleNames.Registered);
                    if (registeredRole != null && roleIds.Contains(registeredRole.Id))
                    {
                        var usedTimes = (await GetAllDiscountUsageHistoryAsync(discount.Id, customer.Id, pageSize: 1)).TotalCount;
                        if (usedTimes >= discount.LimitationTimes)
                        {
                            result.Errors = [await _localizationService.GetResourceAsync("ShoppingCart.Discount.CannotBeUsedAnymore")];
                            return result;
                        }
                    }
                    break;
                }
        }

        // discount requirements — evaluate hierarchical AND/OR tree
        var topLevelRequirements = await GetAllDiscountRequirementsAsync(discount.Id, topLevelOnly: true);
        var topLevelGroup = topLevelRequirements.FirstOrDefault();
        if (topLevelGroup == null || (topLevelGroup.IsGroup && !HasChildRequirements(topLevelGroup.Id)) || !topLevelGroup.InteractionType.HasValue)
        {
            // no requirements → valid
            result.IsValid = true;
            return result;
        }

        var errors = new List<string>();
        result.IsValid = await EvaluateRequirementsAsync(topLevelGroup, topLevelGroup.InteractionType.Value, customer, errors);
        if (!result.IsValid)
            result.Errors = errors;

        return result;
    }

    private bool HasChildRequirements(int parentId)
    {
        return _discountRequirementRepository.TableNoTracking.Any(r => r.ParentId == parentId);
    }

    private async Task<bool> EvaluateRequirementsAsync(DiscountRequirement requirement, RequirementGroupInteractionType groupInteractionType, Customer customer, List<string> errors)
    {
        if (requirement.IsGroup)
        {
            // evaluate children
            var children = _discountRequirementRepository.TableNoTracking
                .Where(r => r.ParentId == requirement.Id)
                .OrderBy(r => r.Id)
                .ToList();

            var interactionType = requirement.InteractionType ?? RequirementGroupInteractionType.And;
            var result = false;

            foreach (var child in children)
            {
                result = await EvaluateRequirementsAsync(child, interactionType, customer, errors);

                if (!result && interactionType == RequirementGroupInteractionType.And)
                    return false;
                if (result && interactionType == RequirementGroupInteractionType.Or)
                    return true;
            }

            return result;
        }

        // leaf requirement — delegate to IDiscountRequirementRule plugin
        // Plugin system [2.10] not built yet — skip unknown rules (treat as passing)
        // When plugin system is built, resolve IDiscountRequirementRule by SystemName and call CheckRequirement
        return true;
    }

    #endregion

    #region Usage history

    public virtual Task<DiscountUsageHistory?> GetDiscountUsageHistoryByIdAsync(int discountUsageHistoryId)
    {
        if (discountUsageHistoryId == 0) return Task.FromResult<DiscountUsageHistory?>(null);
        return Task.FromResult(_discountUsageHistoryRepository.GetById(discountUsageHistoryId));
    }

    public virtual Task<IPagedList<DiscountUsageHistory>> GetAllDiscountUsageHistoryAsync(int? discountId = null,
        int? customerId = null, int? orderId = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _discountUsageHistoryRepository.Table;

        if (discountId.HasValue && discountId.Value > 0)
            query = query.Where(duh => duh.DiscountId == discountId.Value);

        if (customerId.HasValue && customerId.Value > 0)
        {
            // join via Order to filter by customer (no nav properties)
            var orders = _orderRepository.TableNoTracking;
            query = from duh in query
                    join o in orders on duh.OrderId equals o.Id
                    where o.CustomerId == customerId.Value
                    select duh;
        }

        if (orderId.HasValue && orderId.Value > 0)
            query = query.Where(duh => duh.OrderId == orderId.Value);

        query = query.OrderByDescending(duh => duh.CreatedOnUtc);

        IPagedList<DiscountUsageHistory> result = new PagedList<DiscountUsageHistory>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public virtual async Task InsertDiscountUsageHistoryAsync(DiscountUsageHistory discountUsageHistory)
    {
        ArgumentNullException.ThrowIfNull(discountUsageHistory);
        _discountUsageHistoryRepository.Insert(discountUsageHistory);
        await _eventPublisher.EntityInsertedAsync(discountUsageHistory);
    }

    public virtual async Task UpdateDiscountUsageHistoryAsync(DiscountUsageHistory discountUsageHistory)
    {
        ArgumentNullException.ThrowIfNull(discountUsageHistory);
        _discountUsageHistoryRepository.Update(discountUsageHistory);
        await _eventPublisher.EntityUpdatedAsync(discountUsageHistory);
    }

    public virtual async Task DeleteDiscountUsageHistoryAsync(DiscountUsageHistory discountUsageHistory)
    {
        ArgumentNullException.ThrowIfNull(discountUsageHistory);
        _discountUsageHistoryRepository.Delete(discountUsageHistory);
        await _eventPublisher.EntityDeletedAsync(discountUsageHistory);
    }

    #endregion
}
