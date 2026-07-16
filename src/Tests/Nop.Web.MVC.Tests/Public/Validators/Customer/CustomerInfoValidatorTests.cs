using FluentValidation.TestHelper;
using Nop.Core.Domain.Customers;
using Nop.Services.Directory;
using Nop.Web.Models.Customer;
using Nop.Web.Validators.Customer;
using NUnit.Framework;
using Moq;

namespace Nop.Web.MVC.Tests.Public.Validators.Customer
{
    [TestFixture]
    public class CustomerInfoValidatorTests : BaseValidatorTests
    {
        private IStateProvinceService _stateProvinceService;

        [Test]
        public void Should_have_error_when_email_is_null_or_empty()
        {
            _stateProvinceService = new Mock<IStateProvinceService>().Object;

            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, new CustomerSettings());

            var model = new CustomerInfoModel();
            model.Email = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
            model.Email = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }
        [Test]
        public void Should_have_error_when_email_is_wrong_format()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings());

            var model = new CustomerInfoModel();
            model.Email = "adminexample.com";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }
        [Test]
        public void Should_not_have_error_when_email_is_correct_format()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings());

            var model = new CustomerInfoModel();
            model.Email = "admin@example.com";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_firstName_is_null_or_empty()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings());

            var model = new CustomerInfoModel();
            model.FirstName = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FirstName);
            model.FirstName = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FirstName);
        }
        [Test]
        public void Should_not_have_error_when_firstName_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings());

            var model = new CustomerInfoModel();
            model.FirstName = "John";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }

        [Test]
        public void Should_have_error_when_lastName_is_null_or_empty()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings());

            var model = new CustomerInfoModel();
            model.LastName = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.LastName);
            model.LastName = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.LastName);
        }
        [Test]
        public void Should_not_have_error_when_lastName_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings());

            var model = new CustomerInfoModel();
            model.LastName = "Smith";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.LastName);
        }

        [Test]
        public void Should_have_error_when_company_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    CompanyEnabled = true,
                    CompanyRequired = true
                });
            model.Company = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Company);
            model.Company = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Company);


            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    CompanyEnabled = true,
                    CompanyRequired = false
                });
            model.Company = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Company);
            model.Company = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Company);
        }
        [Test]
        public void Should_not_have_error_when_company_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings
                {
                    CompanyEnabled = true
                });

            var model = new CustomerInfoModel();
            model.Company = "Company";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Company);
        }

        [Test]
        public void Should_have_error_when_streetaddress_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    StreetAddressEnabled = true,
                    StreetAddressRequired = true
                });
            model.StreetAddress = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.StreetAddress);
            model.StreetAddress = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.StreetAddress);

            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    StreetAddressEnabled = true,
                    StreetAddressRequired = false
                });
            model.StreetAddress = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.StreetAddress);
            model.StreetAddress = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.StreetAddress);
        }
        [Test]
        public void Should_not_have_error_when_streetaddress_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    StreetAddressEnabled = true
                });

            var model = new CustomerInfoModel();
            model.StreetAddress = "Street address";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.StreetAddress);
        }

        [Test]
        public void Should_have_error_when_streetaddress2_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    StreetAddress2Enabled = true,
                    StreetAddress2Required = true
                });
            model.StreetAddress2 = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.StreetAddress2);
            model.StreetAddress2 = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.StreetAddress2);

            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    StreetAddress2Enabled = true,
                    StreetAddress2Required = false
                });
            model.StreetAddress2 = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.StreetAddress2);
            model.StreetAddress2 = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.StreetAddress2);
        }
        [Test]
        public void Should_not_have_error_when_streetaddress2_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings
                {
                    StreetAddress2Enabled = true
                });

            var model = new CustomerInfoModel();
            model.StreetAddress2 = "Street address 2";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.StreetAddress2);
        }

        [Test]
        public void Should_have_error_when_zippostalcode_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    ZipPostalCodeEnabled = true,
                    ZipPostalCodeRequired = true
                });
            model.ZipPostalCode = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ZipPostalCode);
            model.ZipPostalCode = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ZipPostalCode);


            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    ZipPostalCodeEnabled = true,
                    ZipPostalCodeRequired = false
                });
            model.ZipPostalCode = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ZipPostalCode);
            model.ZipPostalCode = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ZipPostalCode);
        }
        [Test]
        public void Should_not_have_error_when_zippostalcode_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings
                {
                    StreetAddress2Enabled = true
                });

            var model = new CustomerInfoModel();
            model.ZipPostalCode = "zip";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ZipPostalCode);
        }

        [Test]
        public void Should_have_error_when_city_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    CityEnabled = true,
                    CityRequired = true
                });
            model.City = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.City);
            model.City = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.City);


            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    CityEnabled = true,
                    CityRequired = false
                });
            model.City = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.City);
            model.City = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.City);
        }
        [Test]
        public void Should_not_have_error_when_city_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings
                {
                    CityEnabled = true
                });

            var model = new CustomerInfoModel();
            model.City = "City";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.City);
        }

        [Test]
        public void Should_have_error_when_phone_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    PhoneEnabled = true,
                    PhoneRequired = true
                });
            model.Phone = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Phone);
            model.Phone = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Phone);

            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    PhoneEnabled = true,
                    PhoneRequired = false
                });
            model.Phone = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Phone);
            model.Phone = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Phone);
        }
        [Test]
        public void Should_not_have_error_when_phone_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService, 
                new CustomerSettings
                {
                    PhoneEnabled = true
                });

            var model = new CustomerInfoModel();
            model.Phone = "Phone";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Phone);
        }

        [Test]
        public void Should_have_error_when_fax_is_null_or_empty_based_on_required_setting()
        {
            var model = new CustomerInfoModel();

            //required
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    FaxEnabled = true,
                    FaxRequired = true
                });
            model.Fax = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Fax);
            model.Fax = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Fax);


            //not required
            validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    FaxEnabled = true,
                    FaxRequired = false
                });
            model.Fax = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Fax);
            model.Fax = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Fax);
        }
        [Test]
        public void Should_not_have_error_when_fax_is_specified()
        {
            var validator = new CustomerInfoValidator(_localizationService, _stateProvinceService,
                new CustomerSettings
                {
                    FaxEnabled = true
                });

            var model = new CustomerInfoModel();
            model.Fax = "Fax";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Fax);
        }
    }
}
