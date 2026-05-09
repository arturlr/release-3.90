using System.Collections.Generic;
using System.Linq;
using Moq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Discounts;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Services.Tests.Orders
{
    [TestFixture]
    public class OrderTotalCalculationServiceTests : ServiceTest
    {
        private IWorkContext _workContext;
        private IStoreContext _storeContext;
        private ITaxService _taxService;
        private IShippingService _shippingService;
        private IPaymentService _paymentService;
        private ICheckoutAttributeParser _checkoutAttributeParser;
        private Mock<IDiscountService> _discountServiceMock;
        private IGiftCardService _giftCardService;
        private Mock<IGenericAttributeService> _genericAttributeServiceMock;
        private TaxSettings _taxSettings;
        private RewardPointsSettings _rewardPointsSettings;
        private ICategoryService _categoryService;
        private IManufacturerService _manufacturerService;
        private IProductAttributeParser _productAttributeParser;
        private IPriceCalculationService _priceCalcService;
        private IOrderTotalCalculationService _orderTotalCalcService;
        private IAddressService _addressService;
        private ShippingSettings _shippingSettings;
        private ILocalizationService _localizationService;
        private ILogger _logger;
        private IRepository<ShippingMethod> _shippingMethodRepository;
        private IRepository<Warehouse> _warehouseRepository;
        private ShoppingCartSettings _shoppingCartSettings;
        private CatalogSettings _catalogSettings;
        private IEventPublisher _eventPublisher;
        private Store _store;
        private IProductService _productService;
        private IGeoLookupService _geoLookupService;
        private ICountryService _countryService;
        private IStateProvinceService _stateProvinceService;
        private CustomerSettings _customerSettings;
        private AddressSettings _addressSettings;
        private IRewardPointService _rewardPointService;

        [SetUp]
        public new void SetUp()
        {
            _workContext = new Mock<IWorkContext>().Object;

            _store = new Store { Id = 1 };
            var storeContextMock = new Mock<IStoreContext>();
            storeContextMock.Setup(x => x.CurrentStore).Returns(_store);
            _storeContext = storeContextMock.Object;

            _productService = new Mock<IProductService>().Object;

            var pluginFinder = new PluginFinder();
            var cacheManager = new NopNullCache();

            _discountServiceMock = new Mock<IDiscountService>();
            _categoryService = new Mock<ICategoryService>().Object;
            _manufacturerService = new Mock<IManufacturerService>().Object;
            _productAttributeParser = new Mock<IProductAttributeParser>().Object;

            _shoppingCartSettings = new ShoppingCartSettings();
            _catalogSettings = new CatalogSettings();

            _priceCalcService = new PriceCalculationService(_workContext, _storeContext,
                _discountServiceMock.Object, _categoryService, 
                _manufacturerService, _productAttributeParser,
                _productService, cacheManager, 
                _shoppingCartSettings, _catalogSettings);

            var eventPublisherMock = new Mock<IEventPublisher>();
            eventPublisherMock.Setup(x => x.Publish(It.IsAny<object>()));
            _eventPublisher = eventPublisherMock.Object;

            _localizationService = new Mock<ILocalizationService>().Object;

            //shipping
            _shippingSettings = new ShippingSettings();
            _shippingSettings.ActiveShippingRateComputationMethodSystemNames = new List<string>();
            _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add("FixedRateTestShippingRateComputationMethod");
            _shippingMethodRepository = new Mock<IRepository<ShippingMethod>>().Object;
            _warehouseRepository = new Mock<IRepository<Warehouse>>().Object;
            _logger = new NullLogger();

            _paymentService = new Mock<IPaymentService>().Object;
            _checkoutAttributeParser = new Mock<ICheckoutAttributeParser>().Object;
            _giftCardService = new Mock<IGiftCardService>().Object;
            _genericAttributeServiceMock = new Mock<IGenericAttributeService>();

            _shippingService = new ShippingService(_shippingMethodRepository,
                _warehouseRepository,
                _logger,
                _productService,
                _productAttributeParser,
                _checkoutAttributeParser,
                _genericAttributeServiceMock.Object,
                _localizationService,
                _addressService,
                _shippingSettings,
                pluginFinder, 
                _storeContext,
                _eventPublisher, 
                _shoppingCartSettings,
                cacheManager);

            _geoLookupService = new Mock<IGeoLookupService>().Object;
            _countryService = new Mock<ICountryService>().Object;
            _stateProvinceService = new Mock<IStateProvinceService>().Object;
            _customerSettings = new CustomerSettings();
            _addressSettings = new AddressSettings();

            //tax
            _taxSettings = new TaxSettings();
            _taxSettings.ShippingIsTaxable = true;
            _taxSettings.PaymentMethodAdditionalFeeIsTaxable = true;
            _taxSettings.DefaultTaxAddressId = 10;
            var addressServiceMock = new Mock<IAddressService>();
            addressServiceMock.Setup(x => x.GetAddressById(_taxSettings.DefaultTaxAddressId)).Returns(new Address { Id = _taxSettings.DefaultTaxAddressId });
            _addressService = addressServiceMock.Object;
            _taxService = new TaxService(_addressService, _workContext, _storeContext, _taxSettings,
                pluginFinder, _geoLookupService, _countryService, _stateProvinceService, _logger,
                _customerSettings, _shippingSettings, _addressSettings);
            _rewardPointService = new Mock<IRewardPointService>().Object;

            _rewardPointsSettings = new RewardPointsSettings();

            _orderTotalCalcService = new OrderTotalCalculationService(_workContext, _storeContext,
                _priceCalcService, _taxService, _shippingService, _paymentService,
                _checkoutAttributeParser, _discountServiceMock.Object, _giftCardService, _genericAttributeServiceMock.Object,
                _rewardPointService, _taxSettings, _rewardPointsSettings,
                _shippingSettings, _shoppingCartSettings, _catalogSettings);
        }

        [Test]
        public void Can_get_shopping_cart_subTotal_excluding_tax()
        {
            var customer = new Customer();
            var product1 = new Product { Id = 1, Name = "Product name 1", Price = 12.34M, CustomerEntersPrice = false, Published = true };
            var sci1 = new ShoppingCartItem { Product = product1, ProductId = product1.Id, Quantity = 2 };
            var product2 = new Product { Id = 2, Name = "Product name 2", Price = 21.57M, CustomerEntersPrice = false, Published = true };
            var sci2 = new ShoppingCartItem { Product = product2, ProductId = product2.Id, Quantity = 3 };

            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);

            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToCategories, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToManufacturers, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());

            decimal discountAmount;
            List<DiscountForCaching> appliedDiscounts;
            decimal subTotalWithoutDiscount;
            decimal subTotalWithDiscount;
            SortedDictionary<decimal, decimal> taxRates;
            _orderTotalCalcService.GetShoppingCartSubTotal(cart, false,
                out discountAmount, out appliedDiscounts,
                out subTotalWithoutDiscount, out subTotalWithDiscount, out taxRates);
            discountAmount.ShouldEqual(0);
            appliedDiscounts.Count.ShouldEqual(0);
            subTotalWithoutDiscount.ShouldEqual(89.39);
            subTotalWithDiscount.ShouldEqual(89.39);
            taxRates.Count.ShouldEqual(1);
            taxRates.ContainsKey(10).ShouldBeTrue();
            taxRates[10].ShouldEqual(8.939);
        }

        [Test]
        public void Can_get_shopping_cart_subTotal_including_tax()
        {
            var customer = new Customer();
            var product1 = new Product { Id = 1, Name = "Product name 1", Price = 12.34M, CustomerEntersPrice = false, Published = true };
            var sci1 = new ShoppingCartItem { Product= product1, ProductId = product1.Id, Quantity = 2 };
            var product2 = new Product { Id = 2, Name = "Product name 2", Price = 21.57M, CustomerEntersPrice = false, Published = true };
            var sci2 = new ShoppingCartItem { Product = product2, ProductId = product2.Id, Quantity = 3 };

            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);

            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToCategories, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToManufacturers, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());

            decimal discountAmount;
            List<DiscountForCaching> appliedDiscounts;
            decimal subTotalWithoutDiscount;
            decimal subTotalWithDiscount;
            SortedDictionary<decimal, decimal> taxRates;

            _orderTotalCalcService.GetShoppingCartSubTotal(cart, true,
                out discountAmount, out appliedDiscounts,
                out subTotalWithoutDiscount, out subTotalWithDiscount, out taxRates);
            discountAmount.ShouldEqual(0);
            appliedDiscounts.Count.ShouldEqual(0);
            subTotalWithoutDiscount.ShouldEqual(98.329);
            subTotalWithDiscount.ShouldEqual(98.329);
            taxRates.Count.ShouldEqual(1);
            taxRates.ContainsKey(10).ShouldBeTrue();
            taxRates[10].ShouldEqual(8.939);
        }

        [Test]
        public void Can_get_shopping_cart_subTotal_discount_excluding_tax()
        {
            var customer = new Customer();
            var product1 = new Product { Id = 1, Name = "Product name 1", Price = 12.34M, CustomerEntersPrice = false, Published = true };
            var sci1 = new ShoppingCartItem { Product = product1, ProductId = product1.Id, Quantity = 2 };
            var product2 = new Product { Id = 2, Name = "Product name 2", Price = 21.57M, CustomerEntersPrice = false, Published = true };
            var sci2 = new ShoppingCartItem { Product = product2, ProductId = product2.Id, Quantity = 3 };

            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);
            
            var discount1 = new DiscountForCaching { Id = 1, Name = "Discount 1", DiscountType = DiscountType.AssignedToOrderSubTotal, DiscountAmount = 3, DiscountLimitation = DiscountLimitationType.Unlimited };
            _discountServiceMock.Setup(ds => ds.ValidateDiscount(discount1, customer)).Returns(new DiscountValidationResult() { IsValid = true });
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToOrderSubTotal, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching> { discount1 });
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToCategories, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToManufacturers, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());

            decimal discountAmount;
            List<DiscountForCaching> appliedDiscounts;
            decimal subTotalWithoutDiscount;
            decimal subTotalWithDiscount;
            SortedDictionary<decimal, decimal> taxRates;
            _orderTotalCalcService.GetShoppingCartSubTotal(cart, false,
                out discountAmount, out appliedDiscounts,
                out subTotalWithoutDiscount, out subTotalWithDiscount, out taxRates);
            discountAmount.ShouldEqual(3);
            appliedDiscounts.Count.ShouldEqual(1);
            appliedDiscounts.First().Name.ShouldEqual("Discount 1");
            subTotalWithoutDiscount.ShouldEqual(89.39);
            subTotalWithDiscount.ShouldEqual(86.39);
            taxRates.Count.ShouldEqual(1);
            taxRates.ContainsKey(10).ShouldBeTrue();
            taxRates[10].ShouldEqual(8.639);
        }

        [Test]
        public void Can_get_shopping_cart_subTotal_discount_including_tax()
        {
            var customer = new Customer();
            var product1 = new Product { Id = 1, Name = "Product name 1", Price = 12.34M, CustomerEntersPrice = false, Published = true };
            var sci1 = new ShoppingCartItem { Product= product1, ProductId = product1.Id, Quantity = 2 };
            var product2 = new Product { Id = 2, Name = "Product name 2", Price = 21.57M, CustomerEntersPrice = false, Published = true };
            var sci2 = new ShoppingCartItem { Product = product2, ProductId = product2.Id, Quantity = 3 };

            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);

            var discount1 = new DiscountForCaching { Id = 1, Name = "Discount 1", DiscountType = DiscountType.AssignedToOrderSubTotal, DiscountAmount = 3, DiscountLimitation = DiscountLimitationType.Unlimited };
            _discountServiceMock.Setup(ds => ds.ValidateDiscount(discount1, customer)).Returns(new DiscountValidationResult() { IsValid = true });
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToOrderSubTotal, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching> { discount1 });
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToCategories, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());
            _discountServiceMock.Setup(ds => ds.GetAllDiscountsForCaching(DiscountType.AssignedToManufacturers, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<DiscountForCaching>());

            decimal discountAmount;
            List<DiscountForCaching> appliedDiscounts;
            decimal subTotalWithoutDiscount;
            decimal subTotalWithDiscount;
            SortedDictionary<decimal, decimal> taxRates;
            _orderTotalCalcService.GetShoppingCartSubTotal(cart, true,
                out discountAmount, out appliedDiscounts,
                out subTotalWithoutDiscount, out subTotalWithDiscount, out taxRates);

            (System.Math.Round(discountAmount, 10) == 3.3M).ShouldBeTrue();
            appliedDiscounts.Count.ShouldEqual(1);
            appliedDiscounts.First().Name.ShouldEqual("Discount 1");
            subTotalWithoutDiscount.ShouldEqual(98.329);
            subTotalWithDiscount.ShouldEqual(95.029);
            taxRates.Count.ShouldEqual(1);
            taxRates.ContainsKey(10).ShouldBeTrue();
            taxRates[10].ShouldEqual(8.639);
        }

        [Test]
        public void Can_get_shoppingCartItem_additional_shippingCharge()
        {
            var sci1 = new ShoppingCartItem { AttributesXml = "", Quantity = 3, Product= new Product { Weight = 1.5M, Height = 2.5M, Length = 3.5M, Width = 4.5M, AdditionalShippingCharge = 5.5M, IsShipEnabled = true } };
            var sci2 = new ShoppingCartItem { AttributesXml = "", Quantity = 4, Product = new Product { Weight = 11.5M, Height = 12.5M, Length = 13.5M, Width = 14.5M, AdditionalShippingCharge = 6.5M, IsShipEnabled = true } };
            var sci3 = new ShoppingCartItem { AttributesXml = "", Quantity = 5, Product = new Product { Weight = 11.5M, Height = 12.5M, Length = 13.5M, Width = 14.5M, AdditionalShippingCharge = 7.5M, IsShipEnabled = false } };

            var cart = new List<ShoppingCartItem> { sci1, sci2, sci3 };
            _orderTotalCalcService.GetShoppingCartAdditionalShippingCharge(cart).ShouldEqual(42.5M);
        }

        [Test]
        public void Shipping_should_be_free_when_all_shoppingCartItems_are_marked_as_freeShipping()
        {
            var sci1 = new ShoppingCartItem { AttributesXml = "", Quantity = 3, Product = new Product { Weight = 1.5M, Height = 2.5M, Length = 3.5M, Width = 4.5M, IsFreeShipping = true, IsShipEnabled = true } };
            var sci2 = new ShoppingCartItem { AttributesXml = "", Quantity = 4, Product = new Product { Weight = 11.5M, Height = 12.5M, Length = 13.5M, Width = 14.5M, IsFreeShipping = true, IsShipEnabled = true } };
            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            var customer = new Customer();
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);

            _orderTotalCalcService.IsFreeShipping(cart).ShouldEqual(true);
        }

        [Test]
        public void Shipping_should_not_be_free_when_some_of_shoppingCartItems_are_not_marked_as_freeShipping()
        {
            var sci1 = new ShoppingCartItem { AttributesXml = "", Quantity = 3, Product = new Product { Weight = 1.5M, Height = 2.5M, Length = 3.5M, Width = 4.5M, IsFreeShipping = true, IsShipEnabled = true } };
            var sci2 = new ShoppingCartItem { AttributesXml = "", Quantity = 4, Product = new Product { Weight = 11.5M, Height = 12.5M, Length = 13.5M, Width = 14.5M, IsFreeShipping = false, IsShipEnabled = true } };
            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            var customer = new Customer();
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);

            _orderTotalCalcService.IsFreeShipping(cart).ShouldEqual(false);
        }

        [Test]
        public void Shipping_should_be_free_when_customer_is_in_role_with_free_shipping()
        {
            var sci1 = new ShoppingCartItem { AttributesXml = "", Quantity = 3, Product = new Product { Weight = 1.5M, Height = 2.5M, Length = 3.5M, Width = 4.5M, IsFreeShipping = false, IsShipEnabled = true } };
            var sci2 = new ShoppingCartItem { AttributesXml = "", Quantity = 4, Product = new Product { Weight = 11.5M, Height = 12.5M, Length = 13.5M, Width = 14.5M, IsFreeShipping = false, IsShipEnabled = true } };
            var cart = new List<ShoppingCartItem> { sci1, sci2 };
            var customer = new Customer();
            customer.CustomerRoles.Add(new CustomerRole { Active = true, FreeShipping = true });
            customer.CustomerRoles.Add(new CustomerRole { Active = true, FreeShipping = false });
            cart.ForEach(sci => sci.Customer = customer);
            cart.ForEach(sci => sci.CustomerId = customer.Id);

            _orderTotalCalcService.IsFreeShipping(cart).ShouldEqual(true);
        }

        [Test]
        public void Can_convert_reward_points_to_amount()
        {
            _rewardPointsSettings.Enabled = true;
            _rewardPointsSettings.ExchangeRate = 15M;

            _orderTotalCalcService.ConvertRewardPointsToAmount(100).ShouldEqual(1500);
        }

        [Test]
        public void Can_convert_amount_to_reward_points()
        {
            _rewardPointsSettings.Enabled = true;
            _rewardPointsSettings.ExchangeRate = 15M;

            _orderTotalCalcService.ConvertAmountToRewardPoints(100).ShouldEqual(7);
        }

        [Test]
        public void Can_check_minimum_reward_points_to_use_requirement()
        {
            _rewardPointsSettings.Enabled = true;
            _rewardPointsSettings.MinimumRewardPointsToUse = 0;

            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(0).ShouldEqual(true);
            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(1).ShouldEqual(true);
            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(10).ShouldEqual(true);

            _rewardPointsSettings.MinimumRewardPointsToUse = 2;
            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(0).ShouldEqual(false);
            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(1).ShouldEqual(false);
            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(2).ShouldEqual(true);
            _orderTotalCalcService.CheckMinimumRewardPointsToUseRequirement(10).ShouldEqual(true);
        }
    }
}
