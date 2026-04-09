using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;
using Nop.Services.Stores;

namespace Nop.Services.Catalog;

public class CategoryService : ICategoryService
{
    private const string CategoriesByIdKey = "Nop.category.id-{0}";
    private const string CategoriesAllKey = "Nop.category.all-{0}-{1}-{2}";
    private const string CategoriesByParentKey = "Nop.category.byparent-{0}-{1}-{2}";
    private const string CategoriesHomepageKey = "Nop.category.homepage-{0}";
    private const string ProductCategoriesByProductKey = "Nop.productcategory.byproduct-{0}-{1}-{2}";
    private const string CategoriesPrefix = "Nop.category.";
    private const string ProductCategoriesPrefix = "Nop.productcategory.";

    private readonly IRepository<Category> _categoryRepository;
    private readonly IRepository<ProductCategory> _productCategoryRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<AclRecord> _aclRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IWorkContext _workContext;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly IStoreMappingService _storeMappingService;
    private readonly CatalogSettings _catalogSettings;

    public CategoryService(
        IRepository<Category> categoryRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<Product> productRepository,
        IRepository<AclRecord> aclRepository,
        IRepository<StoreMapping> storeMappingRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IWorkContext workContext,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        IStoreMappingService storeMappingService,
        CatalogSettings catalogSettings)
    {
        _categoryRepository = categoryRepository;
        _productCategoryRepository = productCategoryRepository;
        _productRepository = productRepository;
        _aclRepository = aclRepository;
        _storeMappingRepository = storeMappingRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _workContext = workContext;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _storeMappingService = storeMappingService;
        _catalogSettings = catalogSettings;
    }

    public virtual async Task DeleteCategoryAsync(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        category.Deleted = true;
        await UpdateCategoryAsync(category);

        // reset parent for children
        var children = _categoryRepository.Table
            .Where(c => c.ParentCategoryId == category.Id).ToList();
        foreach (var child in children)
        {
            child.ParentCategoryId = 0;
            await UpdateCategoryAsync(child);
        }
    }

    public virtual async Task<IPagedList<Category>> GetAllCategoriesAsync(string categoryName = "", int storeId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        var query = _categoryRepository.TableNoTracking.Where(c => !c.Deleted);

        if (!showHidden)
            query = query.Where(c => c.Published);

        if (!string.IsNullOrWhiteSpace(categoryName))
            query = query.Where(c => c.Name != null && c.Name.Contains(categoryName));

        if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
        {
            query = from c in query
                    join sm in _storeMappingRepository.TableNoTracking
                        on new { c1 = c.Id, c2 = "Category" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into c_sm
                    from sm in c_sm.DefaultIfEmpty()
                    where !c.LimitedToStores || storeId == sm.StoreId
                    select c;
        }

        if (!showHidden && !_catalogSettings.IgnoreAcl)
        {
            var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
                .Where(m => m.CustomerId == _workContext.CurrentCustomer.Id)
                .Select(m => m.CustomerRoleId).ToList();

            query = from c in query
                    join acl in _aclRepository.TableNoTracking
                        on new { c1 = c.Id, c2 = "Category" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into c_acl
                    from acl in c_acl.DefaultIfEmpty()
                    where !c.SubjectToAcl || customerRoleIds.Contains(acl.CustomerRoleId)
                    select c;
        }

        query = from c in query
                group c by c.Id into cGroup
                orderby cGroup.Key
                select cGroup.First();

        query = query.OrderBy(c => c.ParentCategoryId).ThenBy(c => c.DisplayOrder).ThenBy(c => c.Id);

        return await Task.FromResult<IPagedList<Category>>(new PagedList<Category>(query.ToList(), pageIndex, pageSize));
    }

    public virtual async Task<IList<Category>> GetAllCategoriesByParentCategoryIdAsync(int parentCategoryId,
        bool showHidden = false, bool includeAllLevels = false)
    {
        var key = new CacheKey(string.Format(CategoriesByParentKey, parentCategoryId, showHidden, includeAllLevels), CategoriesPrefix);
        return await _cacheManager.GetAsync(key, async () =>
        {
            var query = _categoryRepository.TableNoTracking
                .Where(c => !c.Deleted && c.ParentCategoryId == parentCategoryId);

            if (!showHidden)
                query = query.Where(c => c.Published);

            query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id);
            var categories = query.ToList();

            if (includeAllLevels)
            {
                var childCategories = new List<Category>();
                foreach (var cat in categories)
                {
                    childCategories.AddRange(await GetAllCategoriesByParentCategoryIdAsync(cat.Id, showHidden, true));
                }
                categories.AddRange(childCategories);
            }

            return (IList<Category>)categories;
        }) ?? [];
    }

    public virtual async Task<IList<Category>> GetAllCategoriesDisplayedOnHomePageAsync(bool showHidden = false)
    {
        var key = new CacheKey(string.Format(CategoriesHomepageKey, showHidden), CategoriesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = _categoryRepository.TableNoTracking
                .Where(c => !c.Deleted && c.ShowOnHomePage);

            if (!showHidden)
                query = query.Where(c => c.Published);

            query = query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id);
            return Task.FromResult<IList<Category>>(query.ToList());
        }) ?? [];
    }

    public virtual async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        if (categoryId == 0) return null;
        var key = new CacheKey(string.Format(CategoriesByIdKey, categoryId), CategoriesPrefix);
        return await _cacheManager.GetAsync(key, () => Task.FromResult(_categoryRepository.GetById(categoryId)));
    }

    public virtual async Task InsertCategoryAsync(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        _categoryRepository.Insert(category);
        await _cacheManager.RemoveByPrefixAsync(CategoriesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductCategoriesPrefix);
        await _eventPublisher.EntityInsertedAsync(category);
    }

    public virtual async Task UpdateCategoryAsync(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        _categoryRepository.Update(category);
        await _cacheManager.RemoveByPrefixAsync(CategoriesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductCategoriesPrefix);
        await _eventPublisher.EntityUpdatedAsync(category);
    }

    // ProductCategory mappings

    public virtual async Task DeleteProductCategoryAsync(ProductCategory productCategory)
    {
        ArgumentNullException.ThrowIfNull(productCategory);
        _productCategoryRepository.Delete(productCategory);
        await _cacheManager.RemoveByPrefixAsync(CategoriesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductCategoriesPrefix);
        await _eventPublisher.EntityDeletedAsync(productCategory);
    }

    public virtual Task<IPagedList<ProductCategory>> GetProductCategoriesByCategoryIdAsync(int categoryId,
        int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
    {
        if (categoryId == 0)
            return Task.FromResult<IPagedList<ProductCategory>>(new PagedList<ProductCategory>([], pageIndex, pageSize));

        var query = from pc in _productCategoryRepository.TableNoTracking
                    join p in _productRepository.TableNoTracking on pc.ProductId equals p.Id
                    where pc.CategoryId == categoryId && !p.Deleted && (showHidden || p.Published)
                    orderby pc.DisplayOrder, pc.Id
                    select pc;

        return Task.FromResult<IPagedList<ProductCategory>>(new PagedList<ProductCategory>(query.ToList(), pageIndex, pageSize));
    }

    public virtual async Task<IList<ProductCategory>> GetProductCategoriesByProductIdAsync(int productId, bool showHidden = false)
    {
        return await GetProductCategoriesByProductIdAsync(productId, 0, showHidden);
    }

    public virtual async Task<IList<ProductCategory>> GetProductCategoriesByProductIdAsync(int productId, int storeId, bool showHidden = false)
    {
        if (productId == 0) return [];

        var key = new CacheKey(string.Format(ProductCategoriesByProductKey, productId, storeId, showHidden), ProductCategoriesPrefix);
        return await _cacheManager.GetAsync(key, () =>
        {
            var query = from pc in _productCategoryRepository.TableNoTracking
                        join c in _categoryRepository.TableNoTracking on pc.CategoryId equals c.Id
                        where pc.ProductId == productId && !c.Deleted && (showHidden || c.Published)
                        orderby pc.DisplayOrder, pc.Id
                        select pc;

            var allProductCategories = query.ToList();

            if (!showHidden && storeId > 0)
            {
                allProductCategories = allProductCategories
                    .Where(pc =>
                    {
                        var cat = _categoryRepository.GetById(pc.CategoryId);
                        return cat != null && _storeMappingService.AuthorizeAsync(cat, storeId).GetAwaiter().GetResult();
                    }).ToList();
            }

            return Task.FromResult<IList<ProductCategory>>(allProductCategories);
        }) ?? [];
    }

    public virtual Task<ProductCategory?> GetProductCategoryByIdAsync(int productCategoryId) =>
        Task.FromResult(productCategoryId == 0 ? null : _productCategoryRepository.GetById(productCategoryId));

    public virtual async Task InsertProductCategoryAsync(ProductCategory productCategory)
    {
        ArgumentNullException.ThrowIfNull(productCategory);
        _productCategoryRepository.Insert(productCategory);
        await _cacheManager.RemoveByPrefixAsync(CategoriesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductCategoriesPrefix);
        await _eventPublisher.EntityInsertedAsync(productCategory);
    }

    public virtual async Task UpdateProductCategoryAsync(ProductCategory productCategory)
    {
        ArgumentNullException.ThrowIfNull(productCategory);
        _productCategoryRepository.Update(productCategory);
        await _cacheManager.RemoveByPrefixAsync(CategoriesPrefix);
        await _cacheManager.RemoveByPrefixAsync(ProductCategoriesPrefix);
        await _eventPublisher.EntityUpdatedAsync(productCategory);
    }

    public virtual Task<string[]> GetNotExistingCategoriesAsync(string[] categoryNames)
    {
        ArgumentNullException.ThrowIfNull(categoryNames);
        var existingNames = _categoryRepository.TableNoTracking
            .Where(c => categoryNames.Contains(c.Name))
            .Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Task.FromResult(categoryNames.Where(n => !existingNames.Contains(n)).ToArray());
    }

    public virtual Task<IDictionary<int, int[]>> GetProductCategoryIdsAsync(int[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        var query = _productCategoryRepository.TableNoTracking
            .Where(pc => productIds.Contains(pc.ProductId))
            .Select(pc => new { pc.ProductId, pc.CategoryId })
            .ToList();

        IDictionary<int, int[]> result = query
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.CategoryId).ToArray());

        return Task.FromResult(result);
    }
}
