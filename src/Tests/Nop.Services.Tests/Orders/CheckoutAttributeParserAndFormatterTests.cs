using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Tests;
using NUnit.Framework;
using NSubstitute;

namespace Nop.Services.Tests.Orders
{
    [TestFixture]
    public class CheckoutAttributeParserAndFormatterTests : ServiceTest
    {
        private IRepository<CheckoutAttribute> _checkoutAttributeRepo;
        private IRepository<CheckoutAttributeValue> _checkoutAttributeValueRepo;
        private IEventPublisher _eventPublisher;
        private IStoreMappingService _storeMappingService;
        private ICheckoutAttributeService _checkoutAttributeService;
        private ICheckoutAttributeParser _checkoutAttributeParser;
        private IWorkContext _workContext;
        private ICurrencyService _currencyService;
        private ITaxService _taxService;
        private IPriceFormatter _priceFormatter;
        private IDownloadService _downloadService;
        private IWebHelper _webHelper;
        private ICheckoutAttributeFormatter _checkoutAttributeFormatter;

        private CheckoutAttribute ca1, ca2, ca3;
        private CheckoutAttributeValue cav1_1, cav1_2, cav2_1, cav2_2;
        
        [SetUp]
        public new void SetUp()
        {
            #region Test data

            //color (dropdownlist)
            ca1 = new CheckoutAttribute
            {
                Id = 1,
                Name= "Color",
                TextPrompt = "Select color:",
                IsRequired = true,
                AttributeControlType = AttributeControlType.DropdownList,
                DisplayOrder = 1,
            };
            cav1_1 = new CheckoutAttributeValue
            {
                Id = 11,
                Name = "Green",
                DisplayOrder = 1,
                CheckoutAttribute = ca1,
                CheckoutAttributeId = ca1.Id,
            };
            cav1_2 = new CheckoutAttributeValue
            {
                Id = 12,
                Name = "Red",
                DisplayOrder = 2,
                CheckoutAttribute = ca1,
                CheckoutAttributeId = ca1.Id,
            };
            ca1.CheckoutAttributeValues.Add(cav1_1);
            ca1.CheckoutAttributeValues.Add(cav1_2);

            //custom option (checkboxes)
            ca2 = new CheckoutAttribute
            {
                Id = 2,
                Name = "Custom option",
                TextPrompt = "Select custom option:",
                IsRequired = true,
                AttributeControlType = AttributeControlType.Checkboxes,
                DisplayOrder = 2,
            };
            cav2_1 = new CheckoutAttributeValue
            {
                Id = 21,
                Name = "Option 1",
                DisplayOrder = 1,
                CheckoutAttribute = ca2,
                CheckoutAttributeId = ca2.Id,
            };
            cav2_2 = new CheckoutAttributeValue
            {
                Id = 22,
                Name = "Option 2",
                DisplayOrder = 2,
                CheckoutAttribute = ca2,
                CheckoutAttributeId = ca2.Id,
            };
            ca2.CheckoutAttributeValues.Add(cav2_1);
            ca2.CheckoutAttributeValues.Add(cav2_2);

            //custom text
            ca3 = new CheckoutAttribute
            {
                Id = 3,
                Name = "Custom text",
                TextPrompt = "Enter custom text:",
                IsRequired = true,
                AttributeControlType = AttributeControlType.MultilineTextbox,
                DisplayOrder = 3,
            };


            #endregion
            
            _checkoutAttributeRepo = Substitute.For<IRepository<CheckoutAttribute>>();
            _checkoutAttributeRepo.Table.Returns(new List<CheckoutAttribute> { ca1, ca2, ca3 }.AsQueryable());
            _checkoutAttributeRepo.GetById(ca1.Id).Returns(ca1);
            _checkoutAttributeRepo.GetById(ca2.Id).Returns(ca2);
            _checkoutAttributeRepo.GetById(ca3.Id).Returns(ca3);

            _checkoutAttributeValueRepo = Substitute.For<IRepository<CheckoutAttributeValue>>();
            _checkoutAttributeValueRepo.Table.Returns(new List<CheckoutAttributeValue> { cav1_1, cav1_2, cav2_1, cav2_2 }.AsQueryable());
            _checkoutAttributeValueRepo.GetById(cav1_1.Id).Returns(cav1_1);
            _checkoutAttributeValueRepo.GetById(cav1_2.Id).Returns(cav1_2);
            _checkoutAttributeValueRepo.GetById(cav2_1.Id).Returns(cav2_1);
            _checkoutAttributeValueRepo.GetById(cav2_2.Id).Returns(cav2_2);

            var cacheManager = new NopNullCache();

            _storeMappingService = Substitute.For<IStoreMappingService>();

            _eventPublisher = Substitute.For<IEventPublisher>();
// NSubstitute: void method - no setup needed

            _checkoutAttributeService = new CheckoutAttributeService(cacheManager,
                _checkoutAttributeRepo,
                _checkoutAttributeValueRepo,
                _storeMappingService,
                _eventPublisher);

            _checkoutAttributeParser = new CheckoutAttributeParser(_checkoutAttributeService);



            var workingLanguage = new Language();
            _workContext = Substitute.For<IWorkContext>();
            _workContext.WorkingLanguage.Returns(workingLanguage);
            _currencyService = Substitute.For<ICurrencyService>();
            _taxService = Substitute.For<ITaxService>();
            _priceFormatter = Substitute.For<IPriceFormatter>();
            _downloadService = Substitute.For<IDownloadService>();
            _webHelper = Substitute.For<IWebHelper>();

            _checkoutAttributeFormatter = new CheckoutAttributeFormatter(_workContext,
                _checkoutAttributeService,
                _checkoutAttributeParser,
                _currencyService,
                _taxService,
                _priceFormatter,
                _downloadService,
                _webHelper);
        }
        
        [Test]
        public void Can_add_and_parse_checkoutAttributes()
        {
            string attributes = "";
            //color: green
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca1, cav1_1.Id.ToString());
            //custom option: option 1, option 2
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_1.Id.ToString());
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_2.Id.ToString());
            //custom text
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca3, "Some custom text goes here");

            var parsed_attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(attributes);
            parsed_attributeValues.Contains(cav1_1).ShouldEqual(true);
            parsed_attributeValues.Contains(cav1_2).ShouldEqual(false);
            parsed_attributeValues.Contains(cav2_1).ShouldEqual(true);
            parsed_attributeValues.Contains(cav2_2).ShouldEqual(true);
            parsed_attributeValues.Contains(cav2_2).ShouldEqual(true);

            var parsedValues = _checkoutAttributeParser.ParseValues(attributes, ca3.Id);
            parsedValues.Count.ShouldEqual(1);
            parsedValues.Contains("Some custom text goes here").ShouldEqual(true);
            parsedValues.Contains("Some other custom text").ShouldEqual(false);
        }

        [Test]
        public void Can_add_render_attributes_withoutPrices()
        {
            string attributes = "";
            //color: green
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca1, cav1_1.Id.ToString());
            //custom option: option 1, option 2
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_1.Id.ToString());
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_2.Id.ToString());
            //custom text
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca3, "Some custom text goes here");


            var customer = new Customer();
            string formattedAttributes = _checkoutAttributeFormatter.FormatAttributes(attributes, customer, "<br />", false, false);
            formattedAttributes.ShouldEqual("Color: Green<br />Custom option: Option 1<br />Custom option: Option 2<br />Custom text: Some custom text goes here");
        }

        [Test]
        public void Can_add_and_remove_checkoutAttributes()
        {
            string attributes = "";
            //color: green
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca1, cav1_1.Id.ToString());
            //custom option: option 1, option 2
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_1.Id.ToString());
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca2, cav2_2.Id.ToString());
            //custom text
            attributes = _checkoutAttributeParser.AddCheckoutAttribute(attributes, ca3, "Some custom text goes here");
            //delete some of them
            attributes = _checkoutAttributeParser.RemoveCheckoutAttribute(attributes, ca2);
            attributes = _checkoutAttributeParser.RemoveCheckoutAttribute(attributes, ca3);

            var parsed_attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(attributes);
            parsed_attributeValues.Contains(cav1_1).ShouldEqual(true);
            parsed_attributeValues.Contains(cav1_2).ShouldEqual(false);
            parsed_attributeValues.Contains(cav2_1).ShouldEqual(false);
            parsed_attributeValues.Contains(cav2_2).ShouldEqual(false);
            parsed_attributeValues.Contains(cav2_2).ShouldEqual(false);

            var parsedValues = _checkoutAttributeParser.ParseValues(attributes, ca3.Id);
            parsedValues.Count.ShouldEqual(0);
        }
    }
}
