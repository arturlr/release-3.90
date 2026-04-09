using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Affiliates;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Services.Vendors;
using System.Globalization;

namespace Nop.Services.Orders;

/// <summary>
/// Order processing service — PlaceOrder, status transitions, payment operations.
/// Split into partial class files for manageability.
/// </summary>
public partial class OrderProcessingService(
    IOrderService orderService,
    IWebHelper webHelper,
    ILocalizationService localizationService,
    ILanguageService languageService,
    IProductService productService,
    IPaymentService paymentService,
    INopLogger logger,
    IOrderTotalCalculationService orderTotalCalculationService,
    IPriceCalculationService priceCalculationService,
    IProductAttributeParser productAttributeParser,
    IProductAttributeFormatter productAttributeFormatter,
    IGiftCardService giftCardService,
    IShoppingCartService shoppingCartService,
    ICheckoutAttributeFormatter checkoutAttributeFormatter,
    IShipmentService shipmentService,
    ITaxService taxService,
    ICustomerService customerService,
    IDiscountService discountService,
    IEncryptionService encryptionService,
    IWorkContext workContext,
    IWorkflowMessageService workflowMessageService,
    IVendorService vendorService,
    ICustomerActivityService customerActivityService,
    ICurrencyService currencyService,
    IAffiliateService affiliateService,
    IEventPublisher eventPublisher,
    IRewardPointService rewardPointService,
    IGenericAttributeService genericAttributeService,
    IAddressService addressService,
    ICustomNumberFormatter customNumberFormatter,
    OrderSettings orderSettings,
    PaymentSettings paymentSettings,
    RewardPointsSettings rewardPointsSettings,
    TaxSettings taxSettings,
    LocalizationSettings localizationSettings,
    CurrencySettings currencySettings,
    ShippingSettings shippingSettings) : IOrderProcessingService
{
    /// <summary>
    /// Internal container for order placement details.
    /// </summary>
    protected sealed class PlaceOrderContainer
    {
        public Customer Customer { get; set; } = null!;
        public Language CustomerLanguage { get; set; } = null!;
        public int AffiliateId { get; set; }
        public TaxDisplayType CustomerTaxDisplayType { get; set; }
        public string CustomerCurrencyCode { get; set; } = string.Empty;
        public decimal CustomerCurrencyRate { get; set; }

        public int BillingAddressId { get; set; }
        public int? ShippingAddressId { get; set; }
        public ShippingStatus ShippingStatus { get; set; }
        public string? ShippingMethodName { get; set; }
        public string? ShippingRateComputationMethodSystemName { get; set; }
        public bool PickUpInStore { get; set; }
        public int? PickupAddressId { get; set; }

        public bool IsRecurringShoppingCart { get; set; }
        public Order? InitialOrder { get; set; }

        public string? CheckoutAttributeDescription { get; set; }
        public string? CheckoutAttributesXml { get; set; }

        public IList<ShoppingCartItem> Cart { get; set; } = [];
        public List<Discount> AppliedDiscounts { get; set; } = [];
        public List<AppliedGiftCard> AppliedGiftCards { get; set; } = [];

        public decimal OrderSubTotalInclTax { get; set; }
        public decimal OrderSubTotalExclTax { get; set; }
        public decimal OrderSubTotalDiscountInclTax { get; set; }
        public decimal OrderSubTotalDiscountExclTax { get; set; }
        public decimal OrderShippingTotalInclTax { get; set; }
        public decimal OrderShippingTotalExclTax { get; set; }
        public decimal PaymentAdditionalFeeInclTax { get; set; }
        public decimal PaymentAdditionalFeeExclTax { get; set; }
        public decimal OrderTaxTotal { get; set; }
        public string? VatNumber { get; set; }
        public string? TaxRates { get; set; }
        public decimal OrderDiscountAmount { get; set; }
        public int RedeemedRewardPoints { get; set; }
        public decimal RedeemedRewardPointsAmount { get; set; }
        public decimal OrderTotal { get; set; }
    }
}
