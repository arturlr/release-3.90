using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Catalog;

public interface IProductService
{
    // Products
    Task DeleteProductAsync(Product product);
    Task DeleteProductsAsync(IList<Product> products);
    Task<IList<Product>> GetAllProductsDisplayedOnHomePageAsync();
    Task<Product?> GetProductByIdAsync(int productId);
    Task<IList<Product>> GetProductsByIdsAsync(int[] productIds);
    Task InsertProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task UpdateProductsAsync(IList<Product> products);
    Task<int> GetNumberOfProductsInCategoryAsync(IList<int>? categoryIds = null, int storeId = 0);

    Task<IPagedList<Product>> SearchProductsAsync(
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
        bool showHidden = false, bool? overridePublished = null);

    Task<(IPagedList<Product> Products, IList<int> FilterableSpecificationAttributeOptionIds)> SearchProductsAsync(
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
        bool showHidden = false, bool? overridePublished = null);

    Task<IPagedList<Product>> GetProductsByProductAttributeIdAsync(int productAttributeId, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<IList<Product>> GetAssociatedProductsAsync(int parentGroupedProductId, int storeId = 0, int vendorId = 0, bool showHidden = false);
    Task UpdateProductReviewTotalsAsync(Product product);
    Task<IPagedList<Product>> GetLowStockProductsAsync(int vendorId = 0, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<IPagedList<ProductAttributeCombination>> GetLowStockProductCombinationsAsync(int vendorId = 0, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<Product?> GetProductBySkuAsync(string sku);
    Task<IList<Product>> GetProductsBySkuAsync(string[] skuArray, int vendorId = 0);
    Task UpdateHasTierPricesPropertyAsync(Product product);
    Task UpdateHasDiscountsAppliedAsync(Product product);
    Task<int> GetNumberOfProductsByVendorIdAsync(int vendorId);

    // Inventory
    Task AdjustInventoryAsync(Product product, int quantityToChange, string attributesXml = "", string message = "");
    Task BookReservedInventoryAsync(Product product, int warehouseId, int quantity, string message = "");

    // Related products
    Task DeleteRelatedProductAsync(RelatedProduct relatedProduct);
    Task<IList<RelatedProduct>> GetRelatedProductsByProductId1Async(int productId1, bool showHidden = false);
    Task<RelatedProduct?> GetRelatedProductByIdAsync(int relatedProductId);
    Task InsertRelatedProductAsync(RelatedProduct relatedProduct);
    Task UpdateRelatedProductAsync(RelatedProduct relatedProduct);

    // Cross-sell products
    Task DeleteCrossSellProductAsync(CrossSellProduct crossSellProduct);
    Task<IList<CrossSellProduct>> GetCrossSellProductsByProductId1Async(int productId1, bool showHidden = false);
    Task<CrossSellProduct?> GetCrossSellProductByIdAsync(int crossSellProductId);
    Task InsertCrossSellProductAsync(CrossSellProduct crossSellProduct);
    Task UpdateCrossSellProductAsync(CrossSellProduct crossSellProduct);
    Task<IList<Product>> GetCrossSellProductsByShoppingCartAsync(IList<ShoppingCartItem> cart, int numberOfProducts);

    // Tier prices
    Task DeleteTierPriceAsync(TierPrice tierPrice);
    Task<TierPrice?> GetTierPriceByIdAsync(int tierPriceId);
    Task InsertTierPriceAsync(TierPrice tierPrice);
    Task UpdateTierPriceAsync(TierPrice tierPrice);

    // Product pictures
    Task DeleteProductPictureAsync(ProductPicture productPicture);
    Task<IList<ProductPicture>> GetProductPicturesByProductIdAsync(int productId);
    Task<ProductPicture?> GetProductPictureByIdAsync(int productPictureId);
    Task InsertProductPictureAsync(ProductPicture productPicture);
    Task UpdateProductPictureAsync(ProductPicture productPicture);
    Task<IDictionary<int, int[]>> GetProductsImagesIdsAsync(int[] productsIds);

    // Product reviews
    Task<IPagedList<ProductReview>> GetAllProductReviewsAsync(int customerId, bool? approved,
        DateTime? fromUtc = null, DateTime? toUtc = null, string? message = null,
        int storeId = 0, int productId = 0, int vendorId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task<ProductReview?> GetProductReviewByIdAsync(int productReviewId);
    Task<IList<ProductReview>> GetProductReviewsByIdsAsync(int[] productReviewIds);
    Task DeleteProductReviewAsync(ProductReview productReview);
    Task DeleteProductReviewsAsync(IList<ProductReview> productReviews);

    // Product warehouse inventory
    Task DeleteProductWarehouseInventoryAsync(ProductWarehouseInventory pwi);

    // Stock quantity history
    Task AddStockQuantityHistoryEntryAsync(Product product, int quantityAdjustment, int stockQuantity,
        int warehouseId = 0, string message = "", int? combinationId = null);
    Task<IPagedList<StockQuantityHistory>> GetStockQuantityHistoryAsync(Product product, int warehouseId = 0,
        int combinationId = 0, int pageIndex = 0, int pageSize = int.MaxValue);
}
