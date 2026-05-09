using System.Collections.Generic;
using System.Linq;
using Moq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Services.Tests.Shipping
{
    [TestFixture]
    public class ShippingServiceTests : ServiceTest
    {
        private IRepository<ShippingMethod> _shippingMethodRepository;
        private IRepository<Warehouse> _warehouseRepository;
        private ILogger _logger;
        private IProductAttributeParser _productAttributeParser;
        private ICheckoutAttributeParser _checkoutAttributeParser;
        private ShippingSettings _shippingSettings;
        private IEventPublisher _eventPublisher;
        private ILocalizationService _localizationService;
        private IAddressService _addressService;
        private IGenericAttributeService _genericAttributeService;
        private IShippingService _shippingService;
        private ShoppingCartSettings _shoppingCartSettings;
        private IProductService _productService;
        private Store _store;
        private IStoreContext _storeContext;

        [SetUp]
        public new void SetUp()
        {
            _shippingSettings = new ShippingSettings();
            _shippingSettings.ActiveShippingRateComputationMethodSystemNames = new List<string>();
            _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Add("FixedRateTestShippingRateComputationMethod");

            _shippingMethodRepository = new Mock<IRepository<ShippingMethod>>().Object;
            _warehouseRepository = new Mock<IRepository<Warehouse>>().Object;
            _logger = new NullLogger();
            _productAttributeParser = new Mock<IProductAttributeParser>().Object;
            _checkoutAttributeParser = new Mock<ICheckoutAttributeParser>().Object;

            var cacheManager = new NopNullCache();

            var pluginFinder = new PluginFinder();
            _productService = new Mock<IProductService>().Object;

            var eventPublisherMock = new Mock<IEventPublisher>();
            eventPublisherMock.Setup(x => x.Publish(It.IsAny<object>()));
            _eventPublisher = eventPublisherMock.Object;

            _localizationService = new Mock<ILocalizationService>().Object;
            _addressService = new Mock<IAddressService>().Object;
            _genericAttributeService = new Mock<IGenericAttributeService>().Object;

            _store = new Store { Id = 1 };
            var storeContextMock = new Mock<IStoreContext>();
            storeContextMock.Setup(x => x.CurrentStore).Returns(_store);
            _storeContext = storeContextMock.Object;

            _shoppingCartSettings = new ShoppingCartSettings();
            _shippingService = new ShippingService(_shippingMethodRepository,
                _warehouseRepository,
                _logger,
                _productService,
                _productAttributeParser,
                _checkoutAttributeParser,
                _genericAttributeService,
                _localizationService,
                _addressService,
                _shippingSettings, 
                pluginFinder,
                _storeContext,
                _eventPublisher,
                _shoppingCartSettings,
                cacheManager);
        }

        [Test]
        public void Can_load_shippingRateComputationMethods()
        {
            var srcm = _shippingService.LoadAllShippingRateComputationMethods();
            srcm.ShouldNotBeNull();
            (srcm.Any()).ShouldBeTrue();
        }

        [Test]
        public void Can_load_shippingRateComputationMethod_by_systemKeyword()
        {
            var srcm = _shippingService.LoadShippingRateComputationMethodBySystemName("FixedRateTestShippingRateComputationMethod");
            srcm.ShouldNotBeNull();
        }

        [Test]
        public void Can_load_active_shippingRateComputationMethods()
        {
            var srcm = _shippingService.LoadActiveShippingRateComputationMethods();
            srcm.ShouldNotBeNull();
            (srcm.Any()).ShouldBeTrue();
        }
        
        [Test]
        public void Can_get_shoppingCart_totalWeight_without_attributes()
        {
            var request = new GetShippingOptionRequest
            {
                Items =
                {
                    new GetShippingOptionRequest.PackageItem(new ShoppingCartItem
                    {
                        AttributesXml = "",
                        Quantity = 3,
                        Product = new Product
                        {
                            Weight = 1.5M,
                            Height = 2.5M,
                            Length = 3.5M,
                            Width = 4.5M
                        }
                    }),
                    new GetShippingOptionRequest.PackageItem(new ShoppingCartItem
                    {
                        AttributesXml = "",
                        Quantity = 4,
                        Product = new Product
                        {
                            Weight = 11.5M,
                            Height = 12.5M,
                            Length = 13.5M,
                            Width = 14.5M
                        }
                    })
                }
            };
            _shippingService.GetTotalWeight(request).ShouldEqual(50.5M);
        }
    }
}
