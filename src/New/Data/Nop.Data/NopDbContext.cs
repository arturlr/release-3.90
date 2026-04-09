using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Configuration;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Gdpr;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Polls;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tasks;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Topics;
using Nop.Core.Domain.Vendors;

namespace Nop.Data;

public class NopDbContext : DbContext
{
    public NopDbContext(DbContextOptions<NopDbContext> options) : base(options)
    {
    }

    // Affiliates
    public DbSet<Affiliate> Affiliates => Set<Affiliate>();

    // Blogs
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();

    // Catalog
    public DbSet<BackInStockSubscription> BackInStockSubscriptions => Set<BackInStockSubscription>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryTemplate> CategoryTemplates => Set<CategoryTemplate>();
    public DbSet<CrossSellProduct> CrossSellProducts => Set<CrossSellProduct>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<ManufacturerTemplate> ManufacturerTemplates => Set<ManufacturerTemplate>();
    public DbSet<PredefinedProductAttributeValue> PredefinedProductAttributeValues => Set<PredefinedProductAttributeValue>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeCombination> ProductAttributeCombinations => Set<ProductAttributeCombination>();
    public DbSet<ProductAttributeMapping> ProductAttributeMappings => Set<ProductAttributeMapping>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductManufacturer> ProductManufacturers => Set<ProductManufacturer>();
    public DbSet<ProductPicture> ProductPictures => Set<ProductPicture>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductReviewHelpfulness> ProductReviewHelpfulness => Set<ProductReviewHelpfulness>();
    public DbSet<ProductSpecificationAttribute> ProductSpecificationAttributes => Set<ProductSpecificationAttribute>();
    public DbSet<ProductTag> ProductTags => Set<ProductTag>();
    public DbSet<ProductTemplate> ProductTemplates => Set<ProductTemplate>();
    public DbSet<ProductWarehouseInventory> ProductWarehouseInventory => Set<ProductWarehouseInventory>();
    public DbSet<RelatedProduct> RelatedProducts => Set<RelatedProduct>();
    public DbSet<SpecificationAttribute> SpecificationAttributes => Set<SpecificationAttribute>();
    public DbSet<SpecificationAttributeOption> SpecificationAttributeOptions => Set<SpecificationAttributeOption>();
    public DbSet<StockQuantityHistory> StockQuantityHistory => Set<StockQuantityHistory>();
    public DbSet<TierPrice> TierPrices => Set<TierPrice>();

    // Common
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<AddressAttribute> AddressAttributes => Set<AddressAttribute>();
    public DbSet<AddressAttributeValue> AddressAttributeValues => Set<AddressAttributeValue>();
    public DbSet<GenericAttribute> GenericAttributes => Set<GenericAttribute>();
    public DbSet<SearchTerm> SearchTerms => Set<SearchTerm>();

    // Configuration
    public DbSet<Setting> Settings => Set<Setting>();

    // Customers
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAttribute> CustomerAttributes => Set<CustomerAttribute>();
    public DbSet<CustomerAttributeValue> CustomerAttributeValues => Set<CustomerAttributeValue>();
    public DbSet<CustomerPassword> CustomerPasswords => Set<CustomerPassword>();
    public DbSet<CustomerRole> CustomerRoles => Set<CustomerRole>();
    public DbSet<CustomerCustomerRoleMapping> CustomerCustomerRoleMappings => Set<CustomerCustomerRoleMapping>();
    public DbSet<ExternalAuthenticationRecord> ExternalAuthenticationRecords => Set<ExternalAuthenticationRecord>();
    public DbSet<RewardPointsHistory> RewardPointsHistory => Set<RewardPointsHistory>();

    // Directory
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<MeasureDimension> MeasureDimensions => Set<MeasureDimension>();
    public DbSet<MeasureWeight> MeasureWeights => Set<MeasureWeight>();
    public DbSet<StateProvince> StateProvinces => Set<StateProvince>();

    // Discounts
    public DbSet<Discount> Discounts => Set<Discount>();
    public DbSet<DiscountRequirement> DiscountRequirements => Set<DiscountRequirement>();
    public DbSet<DiscountUsageHistory> DiscountUsageHistory => Set<DiscountUsageHistory>();

    // Gdpr
    public DbSet<GdprLog> GdprLogs => Set<GdprLog>();

    // Forums
    public DbSet<Forum> Forums => Set<Forum>();
    public DbSet<ForumGroup> ForumGroups => Set<ForumGroup>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumPostVote> ForumPostVotes => Set<ForumPostVote>();
    public DbSet<ForumSubscription> ForumSubscriptions => Set<ForumSubscription>();
    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();
    public DbSet<PrivateMessage> PrivateMessages => Set<PrivateMessage>();

    // Localization
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<LocaleStringResource> LocaleStringResources => Set<LocaleStringResource>();
    public DbSet<LocalizedProperty> LocalizedProperties => Set<LocalizedProperty>();

    // Logging
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<ActivityLogType> ActivityLogTypes => Set<ActivityLogType>();
    public DbSet<Log> Logs => Set<Log>();

    // Media
    public DbSet<Download> Downloads => Set<Download>();
    public DbSet<Picture> Pictures => Set<Picture>();

    // Messages
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<NewsLetterSubscription> NewsLetterSubscriptions => Set<NewsLetterSubscription>();
    public DbSet<QueuedEmail> QueuedEmails => Set<QueuedEmail>();

    // News
    public DbSet<NewsComment> NewsComments => Set<NewsComment>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();

    // Orders
    public DbSet<CheckoutAttribute> CheckoutAttributes => Set<CheckoutAttribute>();
    public DbSet<CheckoutAttributeValue> CheckoutAttributeValues => Set<CheckoutAttributeValue>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<GiftCardUsageHistory> GiftCardUsageHistory => Set<GiftCardUsageHistory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<RecurringPayment> RecurringPayments => Set<RecurringPayment>();
    public DbSet<RecurringPaymentHistory> RecurringPaymentHistory => Set<RecurringPaymentHistory>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<ReturnRequestAction> ReturnRequestActions => Set<ReturnRequestAction>();
    public DbSet<ReturnRequestReason> ReturnRequestReasons => Set<ReturnRequestReason>();
    public DbSet<ShoppingCartItem> ShoppingCartItems => Set<ShoppingCartItem>();

    // Polls
    public DbSet<Poll> Polls => Set<Poll>();
    public DbSet<PollAnswer> PollAnswers => Set<PollAnswer>();
    public DbSet<PollVotingRecord> PollVotingRecords => Set<PollVotingRecord>();

    // Security
    public DbSet<AclRecord> AclRecords => Set<AclRecord>();
    public DbSet<PermissionRecord> PermissionRecords => Set<PermissionRecord>();
    public DbSet<PermissionRecordRoleMapping> PermissionRecordRoleMappings => Set<PermissionRecordRoleMapping>();

    // Seo
    public DbSet<UrlRecord> UrlRecords => Set<UrlRecord>();

    // Shipping
    public DbSet<DeliveryDate> DeliveryDates => Set<DeliveryDate>();
    public DbSet<ProductAvailabilityRange> ProductAvailabilityRanges => Set<ProductAvailabilityRange>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => Set<ShipmentItem>();
    public DbSet<ShippingMethod> ShippingMethods => Set<ShippingMethod>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    // Stores
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<StoreMapping> StoreMappings => Set<StoreMapping>();

    // Tasks
    public DbSet<ScheduleTask> ScheduleTasks => Set<ScheduleTask>();

    // Tax
    public DbSet<TaxCategory> TaxCategories => Set<TaxCategory>();

    // Topics
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TopicTemplate> TopicTemplates => Set<TopicTemplate>();

    // Vendors
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorNote> VendorNotes => Set<VendorNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }
}
