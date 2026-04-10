using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Services.Events;

namespace Nop.Services.Catalog;

public class ProductService : IProductService
{
    private const string ProductsByIdKey = "Nop.product.id-{0}";
    private const string ProductsPrefix = "Nop.product.";

    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<ProductCategory> _productCategoryRepository;
    private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
    private readonly IRepository<ProductPicture> _productPictureRepository;
    private readonly IRepository<ProductReview> _productReviewRepository;
    private readonly IRepository<ProductReviewHelpfulness> _productReviewHelpfulnessRepository;
    private readonly IRepository<RelatedProduct> _relatedProductRepository;
    private readonly IRepository<CrossSellProduct> _crossSellProductRepository;
    private readonly IRepository<TierPrice> _tierPriceRepository;
    private readonly IRepository<ProductWarehouseInventory> _productWarehouseInventoryRepository;
    private readonly IRepository<StockQuantityHistory> _stockQuantityHistoryRepository;
    private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
    private readonly IRepository<ProductAttributeCombination> _productAttributeCombinationRepository;
    private readonly IRepository<ProductProductTagMapping> _productProductTagMappingRepository;
    private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
    private readonly IRepository<AclRecord> _aclRepository;
    private readonly IRepository<StoreMapping> _storeMappingRepository;
    private readonly IRepository<CustomerCustomerRoleMapping> _customerRoleMappingRepository;
    private readonly IRepository<LocalizedProperty> _localizedPropertyRepository;
    private readonly IRepository<ShoppingCartItem> _shoppingCartItemRepository;
    private readonly IWorkContext _workContext;
    private readonly IStaticCacheManager _cacheManager;
    private readonly IEventPublisher _eventPublisher;
    private readonly CatalogSettings _catalogSettings;

    public ProductService(
        IRepository<Product> productRepository,
        IRepository<ProductCategory> productCategoryRepository,
        IRepository<ProductManufacturer> productManufacturerRepository,
        IRepository<ProductPicture> productPictureRepository,
        IRepository<ProductReview> productReviewRepository,
        IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
        IRepository<RelatedProduct> relatedProductRepository,
        IRepository<CrossSellProduct> crossSellProductRepository,
        IRepository<TierPrice> tierPriceRepository,
        IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
        IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
        IRepository<ProductAttributeMapping> productAttributeMappingRepository,
        IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
        IRepository<ProductProductTagMapping> productProductTagMappingRepository,
        IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
        IRepository<AclRecord> aclRepository,
        IRepository<StoreMapping> storeMappingRepository,
        IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
        IRepository<LocalizedProperty> localizedPropertyRepository,
        IRepository<ShoppingCartItem> shoppingCartItemRepository,
        IWorkContext workContext,
        IStaticCacheManager cacheManager,
        IEventPublisher eventPublisher,
        CatalogSettings catalogSettings)
    {
        _productRepository = productRepository;
        _productCategoryRepository = productCategoryRepository;
        _productManufacturerRepository = productManufacturerRepository;
        _productPictureRepository = productPictureRepository;
        _productReviewRepository = productReviewRepository;
        _productReviewHelpfulnessRepository = productReviewHelpfulnessRepository;
        _relatedProductRepository = relatedProductRepository;
        _crossSellProductRepository = crossSellProductRepository;
        _tierPriceRepository = tierPriceRepository;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _stockQuantityHistoryRepository = stockQuantityHistoryRepository;
        _productAttributeMappingRepository = productAttributeMappingRepository;
        _productAttributeCombinationRepository = productAttributeCombinationRepository;
        _productProductTagMappingRepository = productProductTagMappingRepository;
        _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
        _aclRepository = aclRepository;
        _storeMappingRepository = storeMappingRepository;
        _customerRoleMappingRepository = customerRoleMappingRepository;
        _localizedPropertyRepository = localizedPropertyRepository;
        _shoppingCartItemRepository = shoppingCartItemRepository;
        _workContext = workContext;
        _cacheManager = cacheManager;
        _eventPublisher = eventPublisher;
        _catalogSettings = catalogSettings;
    }

    public virtual async Task DeleteProductAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        product.Deleted = true;
        await UpdateProductAsync(product);
    }

    public virtual async Task DeleteProductsAsync(IList<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        foreach (var product in products)
            await DeleteProductAsync(product);
    }

    public virtual Task<IList<Product>> GetAllProductsDisplayedOnHomePageAsync()
    {
        IList<Product> products = _productRepository.TableNoTracking
            .Where(p => p.Published && !p.Deleted && p.ShowOnHomePage)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id).ToList();
        return Task.FromResult(products);
    }

    public virtual async Task<Product?> GetProductByIdAsync(int productId)
    {
        if (productId == 0) return null;
        var key = new CacheKey(string.Format(ProductsByIdKey, productId), ProductsPrefix);
        return await _cacheManager.GetAsync(key, () => Task.FromResult(_productRepository.GetById(productId)));
    }

    public virtual Task<IList<Product>> GetProductsByIdsAsync(int[] productIds)
    {
        ArgumentNullException.ThrowIfNull(productIds);
        IList<Product> products = _productRepository.TableNoTracking
            .Where(p => productIds.Contains(p.Id)).ToList();
        // preserve order of input IDs
        var sorted = productIds.Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null).Cast<Product>().ToList();
        return Task.FromResult<IList<Product>>(sorted);
    }

    public virtual async Task InsertProductAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        _productRepository.Insert(product);
        await _cacheManager.RemoveByPrefixAsync(ProductsPrefix);
        await _eventPublisher.EntityInsertedAsync(product);
    }

    public virtual async Task UpdateProductAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        _productRepository.Update(product);
        await _cacheManager.RemoveByPrefixAsync(ProductsPrefix);
        await _eventPublisher.EntityUpdatedAsync(product);
    }

    public virtual async Task UpdateProductsAsync(IList<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _productRepository.Update(products);
        await _cacheManager.RemoveByPrefixAsync(ProductsPrefix);
        foreach (var product in products)
            await _eventPublisher.EntityUpdatedAsync(product);
    }

    public virtual Task<int> GetNumberOfProductsInCategoryAsync(IList<int>? categoryIds = null, int storeId = 0)
    {
        var query = from p in _productRepository.TableNoTracking
                    where p.Published && !p.Deleted && p.VisibleIndividually
                    select p;

        if (categoryIds?.Count > 0)
        {
            query = from p in query
                    join pc in _productCategoryRepository.TableNoTracking on p.Id equals pc.ProductId
                    where categoryIds.Contains(pc.CategoryId)
                    select p;
        }

        if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
        {
            query = from p in query
                    join sm in _storeMappingRepository.TableNoTracking
                        on new { c1 = p.Id, c2 = "Product" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into p_sm
                    from sm in p_sm.DefaultIfEmpty()
                    where !p.LimitedToStores || storeId == sm.StoreId
                    select p;
        }

        return Task.FromResult(query.Distinct().Count());
    }

    public virtual Task<IPagedList<Product>> SearchProductsAsync(
        int pageIndex = 0, int pageSize = int.MaxValue,
        IList<int>? categoryIds = null, int manufacturerId = 0,
        int storeId = 0, int vendorId = 0, int warehouseId = 0,
        ProductType? productType = null, bool visibleIndividuallyOnly = false,
        bool markedAsNewOnly = false, bool? featuredProducts = null,
        decimal? priceMin = null, decimal? priceMax = null,
        int productTagId = 0, string? keywords = null,
        bool searchDescriptions = false, bool searchManufacturerPartNumber = true,
        bool searchSku = true, bool searchProductTags = false,
        int languageId = 0, IList<int>? filteredSpecs = null,
        ProductSortingEnum orderBy = ProductSortingEnum.Position,
        bool showHidden = false, bool? overridePublished = null)
    {
        var (products, _) = SearchProductsInternal(false, pageIndex, pageSize, categoryIds, manufacturerId,
            storeId, vendorId, warehouseId, productType, visibleIndividuallyOnly, markedAsNewOnly,
            featuredProducts, priceMin, priceMax, productTagId, keywords, searchDescriptions,
            searchManufacturerPartNumber, searchSku, searchProductTags, languageId, filteredSpecs,
            orderBy, showHidden, overridePublished);
        return Task.FromResult(products);
    }

    public virtual Task<(IPagedList<Product> Products, IList<int> FilterableSpecificationAttributeOptionIds)> SearchProductsAsync(
        bool loadFilterableSpecificationAttributeOptionIds,
        int pageIndex = 0, int pageSize = int.MaxValue,
        IList<int>? categoryIds = null, int manufacturerId = 0,
        int storeId = 0, int vendorId = 0, int warehouseId = 0,
        ProductType? productType = null, bool visibleIndividuallyOnly = false,
        bool markedAsNewOnly = false, bool? featuredProducts = null,
        decimal? priceMin = null, decimal? priceMax = null,
        int productTagId = 0, string? keywords = null,
        bool searchDescriptions = false, bool searchManufacturerPartNumber = true,
        bool searchSku = true, bool searchProductTags = false,
        int languageId = 0, IList<int>? filteredSpecs = null,
        ProductSortingEnum orderBy = ProductSortingEnum.Position,
        bool showHidden = false, bool? overridePublished = null)
    {
        return Task.FromResult(SearchProductsInternal(loadFilterableSpecificationAttributeOptionIds, pageIndex, pageSize,
            categoryIds, manufacturerId, storeId, vendorId, warehouseId, productType, visibleIndividuallyOnly,
            markedAsNewOnly, featuredProducts, priceMin, priceMax, productTagId, keywords, searchDescriptions,
            searchManufacturerPartNumber, searchSku, searchProductTags, languageId, filteredSpecs,
            orderBy, showHidden, overridePublished));
    }

    private (IPagedList<Product> Products, IList<int> FilterableSpecificationAttributeOptionIds) SearchProductsInternal(
        bool loadFilterableSpecificationAttributeOptionIds,
        int pageIndex, int pageSize, IList<int>? categoryIds, int manufacturerId,
        int storeId, int vendorId, int warehouseId, ProductType? productType,
        bool visibleIndividuallyOnly, bool markedAsNewOnly, bool? featuredProducts,
        decimal? priceMin, decimal? priceMax, int productTagId, string? keywords,
        bool searchDescriptions, bool searchManufacturerPartNumber, bool searchSku,
        bool searchProductTags, int languageId, IList<int>? filteredSpecs,
        ProductSortingEnum orderBy, bool showHidden, bool? overridePublished)
    {
        categoryIds ??= [];
        filteredSpecs ??= [];

        var query = _productRepository.TableNoTracking.Where(p => !p.Deleted);

        // published
        if (!showHidden)
        {
            if (overridePublished.HasValue)
                query = query.Where(p => p.Published == overridePublished.Value);
            else
                query = query.Where(p => p.Published);
        }

        if (visibleIndividuallyOnly)
            query = query.Where(p => p.VisibleIndividually);

        if (productType.HasValue)
            query = query.Where(p => p.ProductTypeId == (int)productType.Value);

        // date range
        if (!showHidden)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(p =>
                (!p.AvailableStartDateTimeUtc.HasValue || p.AvailableStartDateTimeUtc <= utcNow) &&
                (!p.AvailableEndDateTimeUtc.HasValue || p.AvailableEndDateTimeUtc >= utcNow));
        }

        if (markedAsNewOnly)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(p => p.MarkAsNew &&
                (!p.MarkAsNewStartDateTimeUtc.HasValue || p.MarkAsNewStartDateTimeUtc <= utcNow) &&
                (!p.MarkAsNewEndDateTimeUtc.HasValue || p.MarkAsNewEndDateTimeUtc >= utcNow));
        }

        if (priceMin.HasValue)
            query = query.Where(p => p.Price >= priceMin.Value);
        if (priceMax.HasValue)
            query = query.Where(p => p.Price <= priceMax.Value);

        if (vendorId > 0)
            query = query.Where(p => p.VendorId == vendorId);

        if (warehouseId > 0)
            query = query.Where(p => p.WarehouseId == warehouseId || p.UseMultipleWarehouses);

        // category filter
        if (categoryIds.Count > 0)
        {
            if (featuredProducts.HasValue)
            {
                query = from p in query
                        join pc in _productCategoryRepository.TableNoTracking on p.Id equals pc.ProductId
                        where categoryIds.Contains(pc.CategoryId) && pc.IsFeaturedProduct == featuredProducts.Value
                        select p;
            }
            else
            {
                query = from p in query
                        join pc in _productCategoryRepository.TableNoTracking on p.Id equals pc.ProductId
                        where categoryIds.Contains(pc.CategoryId)
                        select p;
            }
        }

        // manufacturer filter
        if (manufacturerId > 0)
        {
            if (featuredProducts.HasValue)
            {
                query = from p in query
                        join pm in _productManufacturerRepository.TableNoTracking on p.Id equals pm.ProductId
                        where pm.ManufacturerId == manufacturerId && pm.IsFeaturedProduct == featuredProducts.Value
                        select p;
            }
            else
            {
                query = from p in query
                        join pm in _productManufacturerRepository.TableNoTracking on p.Id equals pm.ProductId
                        where pm.ManufacturerId == manufacturerId
                        select p;
            }
        }

        // product tag filter
        if (productTagId > 0)
        {
            query = from p in query
                    join ptm in _productProductTagMappingRepository.TableNoTracking on p.Id equals ptm.ProductId
                    where ptm.ProductTagId == productTagId
                    select p;
        }

        // specification filter
        if (filteredSpecs.Count > 0)
        {
            query = from p in query
                    join psa in _productSpecificationAttributeRepository.TableNoTracking on p.Id equals psa.ProductId
                    where psa.AllowFiltering && filteredSpecs.Contains(psa.SpecificationAttributeOptionId)
                    select p;
        }

        // store mapping
        if (storeId > 0 && !_catalogSettings.IgnoreStoreLimitations)
        {
            query = from p in query
                    join sm in _storeMappingRepository.TableNoTracking
                        on new { c1 = p.Id, c2 = "Product" } equals new { c1 = sm.EntityId, c2 = sm.EntityName } into p_sm
                    from sm in p_sm.DefaultIfEmpty()
                    where !p.LimitedToStores || storeId == sm.StoreId
                    select p;
        }

        // ACL
        if (!showHidden && !_catalogSettings.IgnoreAcl)
        {
            var customerRoleIds = _customerRoleMappingRepository.TableNoTracking
                .Where(m => m.CustomerId == _workContext.CurrentCustomer.Id)
                .Select(m => m.CustomerRoleId).ToList();

            query = from p in query
                    join acl in _aclRepository.TableNoTracking
                        on new { c1 = p.Id, c2 = "Product" } equals new { c1 = acl.EntityId, c2 = acl.EntityName } into p_acl
                    from acl in p_acl.DefaultIfEmpty()
                    where !p.SubjectToAcl || customerRoleIds.Contains(acl.CustomerRoleId)
                    select p;
        }

        // keyword search
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var kw = keywords.Trim();
            query = from p in query
                    where p.Name!.Contains(kw) ||
                          (searchDescriptions && (p.ShortDescription!.Contains(kw) || p.FullDescription!.Contains(kw))) ||
                          (searchManufacturerPartNumber && p.ManufacturerPartNumber!.Contains(kw)) ||
                          (searchSku && p.Sku!.Contains(kw))
                    select p;
        }

        // deduplicate
        query = from p in query
                group p by p.Id into pGroup
                select pGroup.First();

        // ordering
        query = orderBy switch
        {
            ProductSortingEnum.PriceAsc => query.OrderBy(p => p.Price),
            ProductSortingEnum.PriceDesc => query.OrderByDescending(p => p.Price),
            ProductSortingEnum.NameAsc => query.OrderBy(p => p.Name),
            ProductSortingEnum.NameDesc => query.OrderByDescending(p => p.Name),
            ProductSortingEnum.CreatedOn => query.OrderByDescending(p => p.CreatedOnUtc),
            _ => query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id)
        };

        var allProducts = query.ToList();

        // filterable spec attribute option IDs (from all matching products, not just current page)
        IList<int> filterableSpecIds = [];
        if (loadFilterableSpecificationAttributeOptionIds)
        {
            var allProductIds = allProducts.Select(p => p.Id).ToList();
            filterableSpecIds = _productSpecificationAttributeRepository.TableNoTracking
                .Where(psa => allProductIds.Contains(psa.ProductId) && psa.AllowFiltering)
                .Select(psa => psa.SpecificationAttributeOptionId)
                .Distinct().ToList();
        }

        var pagedProducts = new PagedList<Product>(allProducts, pageIndex, pageSize);
        return (pagedProducts, filterableSpecIds);
    }

    public virtual Task<IPagedList<Product>> GetProductsByProductAttributeIdAsync(int productAttributeId, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from p in _productRepository.TableNoTracking
                    join pam in _productAttributeMappingRepository.TableNoTracking on p.Id equals pam.ProductId
                    where pam.ProductAttributeId == productAttributeId && !p.Deleted
                    orderby p.Name
                    select p;
        query = query.Distinct();
        return Task.FromResult<IPagedList<Product>>(new PagedList<Product>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<IList<Product>> GetAssociatedProductsAsync(int parentGroupedProductId, int storeId = 0, int vendorId = 0, bool showHidden = false)
    {
        var query = _productRepository.TableNoTracking
            .Where(p => !p.Deleted && p.ParentGroupedProductId == parentGroupedProductId);
        if (!showHidden)
            query = query.Where(p => p.Published);
        if (vendorId > 0)
            query = query.Where(p => p.VendorId == vendorId);
        query = query.OrderBy(p => p.DisplayOrder).ThenBy(p => p.Id);
        IList<Product> products = query.ToList();
        return Task.FromResult(products);
    }

    public virtual async Task UpdateProductReviewTotalsAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        var approvedReviews = _productReviewRepository.TableNoTracking
            .Where(r => r.ProductId == product.Id && r.IsApproved).ToList();
        product.ApprovedRatingSum = approvedReviews.Sum(r => r.Rating);
        product.NotApprovedRatingSum = _productReviewRepository.TableNoTracking
            .Where(r => r.ProductId == product.Id && !r.IsApproved).Sum(r => r.Rating);
        product.ApprovedTotalReviews = approvedReviews.Count;
        product.NotApprovedTotalReviews = _productReviewRepository.TableNoTracking
            .Count(r => r.ProductId == product.Id && !r.IsApproved);
        await UpdateProductAsync(product);
    }

    public virtual Task<IPagedList<Product>> GetLowStockProductsAsync(int vendorId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _productRepository.TableNoTracking
            .Where(p => !p.Deleted && p.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock &&
                         p.MinStockQuantity >= p.StockQuantity);
        if (vendorId > 0)
            query = query.Where(p => p.VendorId == vendorId);
        query = query.OrderBy(p => p.MinStockQuantity).ThenBy(p => p.Id);
        return Task.FromResult<IPagedList<Product>>(new PagedList<Product>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<IPagedList<ProductAttributeCombination>> GetLowStockProductCombinationsAsync(int vendorId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = from pac in _productAttributeCombinationRepository.TableNoTracking
                    join p in _productRepository.TableNoTracking on pac.ProductId equals p.Id
                    where !p.Deleted && p.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes &&
                          pac.StockQuantity <= pac.NotifyAdminForQuantityBelow
                    select pac;
        if (vendorId > 0)
        {
            query = from pac in query
                    join p in _productRepository.TableNoTracking on pac.ProductId equals p.Id
                    where p.VendorId == vendorId
                    select pac;
        }
        return Task.FromResult<IPagedList<ProductAttributeCombination>>(new PagedList<ProductAttributeCombination>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<Product?> GetProductBySkuAsync(string sku)
    {
        if (string.IsNullOrEmpty(sku)) return Task.FromResult<Product?>(null);
        return Task.FromResult(_productRepository.TableNoTracking.FirstOrDefault(p => p.Sku == sku));
    }

    public virtual Task<IList<Product>> GetProductsBySkuAsync(string[] skuArray, int vendorId = 0)
    {
        ArgumentNullException.ThrowIfNull(skuArray);
        var query = _productRepository.TableNoTracking.Where(p => skuArray.Contains(p.Sku) && !p.Deleted);
        if (vendorId > 0)
            query = query.Where(p => p.VendorId == vendorId);
        IList<Product> products = query.ToList();
        return Task.FromResult(products);
    }

    public virtual async Task UpdateHasTierPricesPropertyAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        product.HasTierPrices = _tierPriceRepository.TableNoTracking.Any(tp => tp.ProductId == product.Id);
        await UpdateProductAsync(product);
    }

    public virtual async Task UpdateHasDiscountsAppliedAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        // HasDiscountsApplied will be set when IDiscountService is built [4.5]
        await UpdateProductAsync(product);
    }

    public virtual Task<int> GetNumberOfProductsByVendorIdAsync(int vendorId) =>
        Task.FromResult(_productRepository.TableNoTracking.Count(p => p.VendorId == vendorId && !p.Deleted));

    // Inventory

    public virtual async Task AdjustInventoryAsync(Product product, int quantityToChange, string attributesXml = "", string message = "")
    {
        ArgumentNullException.ThrowIfNull(product);
        if (quantityToChange == 0) return;

        if (product.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStock)
        {
            product.StockQuantity += quantityToChange;
            await UpdateProductAsync(product);
            await AddStockQuantityHistoryEntryAsync(product, quantityToChange, product.StockQuantity, message: message);
        }
        else if (product.ManageInventoryMethodId == (int)ManageInventoryMethod.ManageStockByAttributes && !string.IsNullOrEmpty(attributesXml))
        {
            var combination = _productAttributeCombinationRepository.Table
                .FirstOrDefault(c => c.ProductId == product.Id && c.AttributesXml == attributesXml);
            if (combination != null)
            {
                combination.StockQuantity += quantityToChange;
                _productAttributeCombinationRepository.Update(combination);
                await AddStockQuantityHistoryEntryAsync(product, quantityToChange, combination.StockQuantity,
                    message: message, combinationId: combination.Id);
            }
        }
    }

    public virtual async Task BookReservedInventoryAsync(Product product, int warehouseId, int quantity, string message = "")
    {
        ArgumentNullException.ThrowIfNull(product);
        if (product.UseMultipleWarehouses)
        {
            var pwi = _productWarehouseInventoryRepository.Table
                .FirstOrDefault(x => x.ProductId == product.Id && x.WarehouseId == warehouseId);
            if (pwi != null)
            {
                pwi.ReservedQuantity = Math.Max(pwi.ReservedQuantity + quantity, 0);
                pwi.StockQuantity += quantity;
                _productWarehouseInventoryRepository.Update(pwi);
            }
        }
        product.StockQuantity += quantity;
        await UpdateProductAsync(product);
        await AddStockQuantityHistoryEntryAsync(product, quantity, product.StockQuantity, warehouseId, message);
    }

    // Related products

    public virtual async Task DeleteRelatedProductAsync(RelatedProduct relatedProduct)
    {
        ArgumentNullException.ThrowIfNull(relatedProduct);
        _relatedProductRepository.Delete(relatedProduct);
        await _eventPublisher.EntityDeletedAsync(relatedProduct);
    }

    public virtual Task<IList<RelatedProduct>> GetRelatedProductsByProductId1Async(int productId1, bool showHidden = false)
    {
        IList<RelatedProduct> related = _relatedProductRepository.TableNoTracking
            .Where(rp => rp.ProductId1 == productId1)
            .OrderBy(rp => rp.DisplayOrder).ThenBy(rp => rp.Id).ToList();
        return Task.FromResult(related);
    }

    public virtual Task<RelatedProduct?> GetRelatedProductByIdAsync(int relatedProductId) =>
        Task.FromResult(relatedProductId == 0 ? null : _relatedProductRepository.GetById(relatedProductId));

    public virtual async Task InsertRelatedProductAsync(RelatedProduct relatedProduct)
    {
        ArgumentNullException.ThrowIfNull(relatedProduct);
        _relatedProductRepository.Insert(relatedProduct);
        await _eventPublisher.EntityInsertedAsync(relatedProduct);
    }

    public virtual async Task UpdateRelatedProductAsync(RelatedProduct relatedProduct)
    {
        ArgumentNullException.ThrowIfNull(relatedProduct);
        _relatedProductRepository.Update(relatedProduct);
        await _eventPublisher.EntityUpdatedAsync(relatedProduct);
    }

    // Cross-sell products

    public virtual async Task DeleteCrossSellProductAsync(CrossSellProduct crossSellProduct)
    {
        ArgumentNullException.ThrowIfNull(crossSellProduct);
        _crossSellProductRepository.Delete(crossSellProduct);
        await _eventPublisher.EntityDeletedAsync(crossSellProduct);
    }

    public virtual Task<IList<CrossSellProduct>> GetCrossSellProductsByProductId1Async(int productId1, bool showHidden = false)
    {
        IList<CrossSellProduct> crossSells = _crossSellProductRepository.TableNoTracking
            .Where(cs => cs.ProductId1 == productId1).ToList();
        return Task.FromResult(crossSells);
    }

    public virtual Task<CrossSellProduct?> GetCrossSellProductByIdAsync(int crossSellProductId) =>
        Task.FromResult(crossSellProductId == 0 ? null : _crossSellProductRepository.GetById(crossSellProductId));

    public virtual async Task InsertCrossSellProductAsync(CrossSellProduct crossSellProduct)
    {
        ArgumentNullException.ThrowIfNull(crossSellProduct);
        _crossSellProductRepository.Insert(crossSellProduct);
        await _eventPublisher.EntityInsertedAsync(crossSellProduct);
    }

    public virtual async Task UpdateCrossSellProductAsync(CrossSellProduct crossSellProduct)
    {
        ArgumentNullException.ThrowIfNull(crossSellProduct);
        _crossSellProductRepository.Update(crossSellProduct);
        await _eventPublisher.EntityUpdatedAsync(crossSellProduct);
    }

    public virtual Task<IList<Product>> GetCrossSellProductsByShoppingCartAsync(IList<ShoppingCartItem> cart, int numberOfProducts)
    {
        var cartProductIds = cart.Select(sci => sci.ProductId).ToHashSet();
        var crossSellProductIds = _crossSellProductRepository.TableNoTracking
            .Where(cs => cartProductIds.Contains(cs.ProductId1))
            .Select(cs => cs.ProductId2)
            .Where(id => !cartProductIds.Contains(id))
            .Distinct().Take(numberOfProducts).ToList();

        IList<Product> products = _productRepository.TableNoTracking
            .Where(p => crossSellProductIds.Contains(p.Id) && !p.Deleted && p.Published).ToList();
        return Task.FromResult(products);
    }

    // Tier prices

    public virtual async Task DeleteTierPriceAsync(TierPrice tierPrice)
    {
        ArgumentNullException.ThrowIfNull(tierPrice);
        _tierPriceRepository.Delete(tierPrice);
        await _cacheManager.RemoveByPrefixAsync(ProductsPrefix);
        await _eventPublisher.EntityDeletedAsync(tierPrice);
    }

    public virtual Task<TierPrice?> GetTierPriceByIdAsync(int tierPriceId) =>
        Task.FromResult(tierPriceId == 0 ? null : _tierPriceRepository.GetById(tierPriceId));

    public virtual async Task InsertTierPriceAsync(TierPrice tierPrice)
    {
        ArgumentNullException.ThrowIfNull(tierPrice);
        _tierPriceRepository.Insert(tierPrice);
        await _cacheManager.RemoveByPrefixAsync(ProductsPrefix);
        await _eventPublisher.EntityInsertedAsync(tierPrice);
    }

    public virtual async Task UpdateTierPriceAsync(TierPrice tierPrice)
    {
        ArgumentNullException.ThrowIfNull(tierPrice);
        _tierPriceRepository.Update(tierPrice);
        await _cacheManager.RemoveByPrefixAsync(ProductsPrefix);
        await _eventPublisher.EntityUpdatedAsync(tierPrice);
    }

    // Product pictures

    public virtual async Task DeleteProductPictureAsync(ProductPicture productPicture)
    {
        ArgumentNullException.ThrowIfNull(productPicture);
        _productPictureRepository.Delete(productPicture);
        await _eventPublisher.EntityDeletedAsync(productPicture);
    }

    public virtual Task<IList<ProductPicture>> GetProductPicturesByProductIdAsync(int productId)
    {
        IList<ProductPicture> pics = _productPictureRepository.TableNoTracking
            .Where(pp => pp.ProductId == productId)
            .OrderBy(pp => pp.DisplayOrder).ThenBy(pp => pp.Id).ToList();
        return Task.FromResult(pics);
    }

    public virtual Task<ProductPicture?> GetProductPictureByIdAsync(int productPictureId) =>
        Task.FromResult(productPictureId == 0 ? null : _productPictureRepository.GetById(productPictureId));

    public virtual async Task InsertProductPictureAsync(ProductPicture productPicture)
    {
        ArgumentNullException.ThrowIfNull(productPicture);
        _productPictureRepository.Insert(productPicture);
        await _eventPublisher.EntityInsertedAsync(productPicture);
    }

    public virtual async Task UpdateProductPictureAsync(ProductPicture productPicture)
    {
        ArgumentNullException.ThrowIfNull(productPicture);
        _productPictureRepository.Update(productPicture);
        await _eventPublisher.EntityUpdatedAsync(productPicture);
    }

    public virtual Task<IDictionary<int, int[]>> GetProductsImagesIdsAsync(int[] productsIds)
    {
        ArgumentNullException.ThrowIfNull(productsIds);
        var query = _productPictureRepository.TableNoTracking
            .Where(pp => productsIds.Contains(pp.ProductId))
            .Select(pp => new { pp.ProductId, pp.PictureId }).ToList();
        IDictionary<int, int[]> result = query
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.PictureId).ToArray());
        return Task.FromResult(result);
    }

    // Product reviews

    public virtual Task<IPagedList<ProductReview>> GetAllProductReviewsAsync(int customerId, bool? approved,
        DateTime? fromUtc = null, DateTime? toUtc = null, string? message = null,
        int storeId = 0, int productId = 0, int vendorId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _productReviewRepository.TableNoTracking.AsQueryable();
        if (approved.HasValue)
            query = query.Where(r => r.IsApproved == approved.Value);
        if (customerId > 0)
            query = query.Where(r => r.CustomerId == customerId);
        if (fromUtc.HasValue)
            query = query.Where(r => r.CreatedOnUtc >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(r => r.CreatedOnUtc <= toUtc.Value);
        if (!string.IsNullOrEmpty(message))
            query = query.Where(r => (r.Title != null && r.Title.Contains(message)) || (r.ReviewText != null && r.ReviewText.Contains(message)));
        if (storeId > 0)
            query = query.Where(r => r.StoreId == storeId);
        if (productId > 0)
            query = query.Where(r => r.ProductId == productId);
        if (vendorId > 0)
        {
            query = from r in query
                    join p in _productRepository.TableNoTracking on r.ProductId equals p.Id
                    where p.VendorId == vendorId
                    select r;
        }
        query = query.OrderByDescending(r => r.CreatedOnUtc).ThenBy(r => r.Id);
        return Task.FromResult<IPagedList<ProductReview>>(new PagedList<ProductReview>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<ProductReview?> GetProductReviewByIdAsync(int productReviewId) =>
        Task.FromResult(productReviewId == 0 ? null : _productReviewRepository.GetById(productReviewId));

    public virtual Task<IList<ProductReview>> GetProductReviewsByIdsAsync(int[] productReviewIds)
    {
        ArgumentNullException.ThrowIfNull(productReviewIds);
        IList<ProductReview> reviews = _productReviewRepository.TableNoTracking
            .Where(r => productReviewIds.Contains(r.Id)).ToList();
        return Task.FromResult(reviews);
    }

    public virtual async Task InsertProductReviewAsync(ProductReview productReview)
    {
        ArgumentNullException.ThrowIfNull(productReview);
        _productReviewRepository.Insert(productReview);
        await _eventPublisher.EntityInsertedAsync(productReview);
    }

    public virtual async Task SetProductReviewHelpfulnessAsync(ProductReview productReview, int customerId, bool wasHelpful)
    {
        ArgumentNullException.ThrowIfNull(productReview);

        var existing = _productReviewHelpfulnessRepository.Table
            .FirstOrDefault(prh => prh.ProductReviewId == productReview.Id && prh.CustomerId == customerId);

        if (existing != null)
        {
            existing.WasHelpful = wasHelpful;
            _productReviewHelpfulnessRepository.Update(existing);
        }
        else
        {
            _productReviewHelpfulnessRepository.Insert(new ProductReviewHelpfulness
            {
                ProductReviewId = productReview.Id,
                CustomerId = customerId,
                WasHelpful = wasHelpful,
            });
        }

        // Recalculate totals
        var allEntries = _productReviewHelpfulnessRepository.TableNoTracking
            .Where(prh => prh.ProductReviewId == productReview.Id).ToList();
        productReview.HelpfulYesTotal = allEntries.Count(x => x.WasHelpful);
        productReview.HelpfulNoTotal = allEntries.Count(x => !x.WasHelpful);
        _productReviewRepository.Update(productReview);

        await Task.CompletedTask;
    }

    public virtual async Task DeleteProductReviewAsync(ProductReview productReview)
    {
        ArgumentNullException.ThrowIfNull(productReview);
        _productReviewRepository.Delete(productReview);
        await _eventPublisher.EntityDeletedAsync(productReview);
    }

    public virtual async Task DeleteProductReviewsAsync(IList<ProductReview> productReviews)
    {
        ArgumentNullException.ThrowIfNull(productReviews);
        _productReviewRepository.Delete(productReviews);
        foreach (var review in productReviews)
            await _eventPublisher.EntityDeletedAsync(review);
    }

    // Product warehouse inventory

    public virtual async Task DeleteProductWarehouseInventoryAsync(ProductWarehouseInventory pwi)
    {
        ArgumentNullException.ThrowIfNull(pwi);
        _productWarehouseInventoryRepository.Delete(pwi);
        await _eventPublisher.EntityDeletedAsync(pwi);
    }

    // Stock quantity history

    public virtual Task AddStockQuantityHistoryEntryAsync(Product product, int quantityAdjustment, int stockQuantity,
        int warehouseId = 0, string message = "", int? combinationId = null)
    {
        ArgumentNullException.ThrowIfNull(product);
        var entry = new StockQuantityHistory
        {
            ProductId = product.Id,
            QuantityAdjustment = quantityAdjustment,
            StockQuantity = stockQuantity,
            WarehouseId = warehouseId,
            Message = message,
            CombinationId = combinationId ?? 0,
            CreatedOnUtc = DateTime.UtcNow
        };
        _stockQuantityHistoryRepository.Insert(entry);
        return Task.CompletedTask;
    }

    public virtual Task<IPagedList<StockQuantityHistory>> GetStockQuantityHistoryAsync(Product product, int warehouseId = 0,
        int combinationId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(product);
        var query = _stockQuantityHistoryRepository.TableNoTracking
            .Where(sqh => sqh.ProductId == product.Id);
        if (warehouseId > 0)
            query = query.Where(sqh => sqh.WarehouseId == warehouseId);
        if (combinationId > 0)
            query = query.Where(sqh => sqh.CombinationId == combinationId);
        query = query.OrderByDescending(sqh => sqh.CreatedOnUtc).ThenByDescending(sqh => sqh.Id);
        return Task.FromResult<IPagedList<StockQuantityHistory>>(new PagedList<StockQuantityHistory>(query.ToList(), pageIndex, pageSize));
    }
}
