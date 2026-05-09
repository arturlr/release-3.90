using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Autofac;
using Moq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Tax;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Tests;
using NUnit.Framework;

namespace Nop.Services.Tests.Catalog
{
    [TestFixture]
    public class PriceFormatterTests : ServiceTest
    {
        private IRepository<Currency> _currencyRepo;
        private IStoreMappingService _storeMappingService;
        private ICurrencyService _currencyService;
        private CurrencySettings _currencySettings;
        private IWorkContext _workContext;
        private Mock<ILocalizationService> _localizationServiceMock;
        private TaxSettings _taxSettings;
        private IPriceFormatter _priceFormatter;
        
        [SetUp]
        public new void SetUp()
        {
            var cacheManager = new NopNullCache();

            var workContextMock = new Mock<IWorkContext>();
            workContextMock.Setup(w => w.WorkingCurrency).Returns(new Currency { RoundingType = RoundingType.Rounding001 });
            _workContext = workContextMock.Object;

            _currencySettings = new CurrencySettings();
            var currency1 = new Currency
            {
                Id = 1,
                Name = "Euro",
                CurrencyCode = "EUR",
                DisplayLocale =  "",
                CustomFormatting = "€0.00",
                DisplayOrder = 1,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc= DateTime.UtcNow
            };
            var currency2 = new Currency
            {
                Id = 1,
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
                CustomFormatting = "",
                DisplayOrder = 2,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc= DateTime.UtcNow
            };

            var currencyRepoMock = new Mock<IRepository<Currency>>();
            currencyRepoMock.Setup(x => x.Table).Returns(new List<Currency> { currency1, currency2 }.AsQueryable());
            _currencyRepo = currencyRepoMock.Object;

            _storeMappingService = new Mock<IStoreMappingService>().Object;

            var pluginFinder = new PluginFinder();
            _currencyService = new CurrencyService(cacheManager, _currencyRepo, _storeMappingService,
                _currencySettings, pluginFinder, null);

            _taxSettings = new TaxSettings();

            _localizationServiceMock = new Mock<ILocalizationService>();
            _localizationServiceMock.Setup(x => x.GetResource("Products.InclTaxSuffix", 1, false, "", false)).Returns("{0} incl tax");
            _localizationServiceMock.Setup(x => x.GetResource("Products.ExclTaxSuffix", 1, false, "", false)).Returns("{0} excl tax");
            
            _priceFormatter = new PriceFormatter(_workContext, _currencyService, _localizationServiceMock.Object, 
                _taxSettings, _currencySettings);

            var nopEngineMock = new Mock<NopEngine>();
            var containerMock = new Mock<IContainer>();
            var containerManagerMock = new Mock<ContainerManager>(containerMock.Object);
            nopEngineMock.Setup(x => x.ContainerManager).Returns(containerManagerMock.Object);
            containerManagerMock.Setup(x => x.Resolve<IWorkContext>(It.IsAny<string>(), It.IsAny<ILifetimeScope>())).Returns(_workContext);
            EngineContext.Replace(nopEngineMock.Object);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            EngineContext.Replace(null);
        }

        [Test]
        public void Can_formatPrice_with_custom_currencyFormatting()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            var currency = new Currency
            {
                Id = 1,
                Name = "Euro",
                CurrencyCode = "EUR",
                DisplayLocale =  "",
                CustomFormatting = "€0.00"
            };
            var language = new Language
            {
                Id = 1,
                Name = "English",
                LanguageCulture = "en-US"
            };
            _priceFormatter.FormatPrice(1234.5M, false, currency, language, false, false).ShouldEqual("€1234.50");
        }

        [Test]
        public void Can_formatPrice_with_distinct_currencyDisplayLocale()
        {
            var usd_currency = new Currency
            {
                Id = 1,
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
            };
            var gbp_currency = new Currency
            {
                Id = 2,
                Name = "great british pound",
                CurrencyCode = "GBP",
                DisplayLocale = "en-GB",
            };
            var language = new Language
            {
                Id = 1,
                Name = "English",
                LanguageCulture = "en-US"
            };
            _priceFormatter.FormatPrice(1234.5M, false, usd_currency, language, false, false).ShouldEqual("$1,234.50");
            _priceFormatter.FormatPrice(1234.5M, false, gbp_currency, language, false, false).ShouldEqual("£1,234.50");
        }

        [Test]
        public void Can_formatPrice_with_showTax()
        {
            var currency = new Currency
            {
                Id = 1,
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
            };
            var language = new Language
            {
                Id = 1,
                Name = "English",
                LanguageCulture = "en-US"
            };
            _priceFormatter.FormatPrice(1234.5M, false, currency, language, true, true).ShouldEqual("$1,234.50 incl tax");
            _priceFormatter.FormatPrice(1234.5M, false, currency, language, false, true).ShouldEqual("$1,234.50 excl tax");

        }

        [Test]
        public void Can_formatPrice_with_showCurrencyCode()
        {
            var currency = new Currency
            {
                Id = 1,
                Name = "US Dollar",
                CurrencyCode = "USD",
                DisplayLocale = "en-US",
            };
            var language = new Language
            {
                Id = 1,
                Name = "English",
                LanguageCulture = "en-US"
            };
            _currencySettings.DisplayCurrencyLabel = true;
            _priceFormatter.FormatPrice(1234.5M, true, currency, language, false, false).ShouldEqual("$1,234.50 (USD)");
            
            _currencySettings.DisplayCurrencyLabel = false;
            _priceFormatter.FormatPrice(1234.5M, true, currency, language, false, false).ShouldEqual("$1,234.50");
        }
    }
}
