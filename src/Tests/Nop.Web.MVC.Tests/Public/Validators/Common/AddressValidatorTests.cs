using FluentValidation.TestHelper;
using Nop.Core.Domain.Common;
using Nop.Services.Directory;
using Nop.Web.Models.Common;
using Nop.Web.Validators.Common;
using NUnit.Framework;
using Moq;

namespace Nop.Web.MVC.Tests.Public.Validators.Common
{
    [TestFixture]
    public class AddressValidatorTests : BaseValidatorTests
    {
        private IStateProvinceService _stateProvinceService;

        [SetUp]
        public new void Setup()
        {
            _stateProvinceService = new Mock<IStateProvinceService>().Object;
        }

        [Test]
        public void Should_have_error_when_email_is_null_or_empty()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.Email = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
            model.Email = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }
        [Test]
        public void Should_have_error_when_email_is_wrong_format()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.Email = "adminexample.com";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }
        [Test]
        public void Should_not_have_error_when_email_is_correct_format()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.Email = "admin@example.com";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_firstName_is_null_or_empty()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.FirstName = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FirstName);
            model.FirstName = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FirstName);
        }
        [Test]
        public void Should_not_have_error_when_firstName_is_specified()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.FirstName = "John";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }

        [Test]
        public void Should_have_error_when_lastName_is_null_or_empty()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.LastName = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.LastName);
            model.LastName = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.LastName);
        }
        [Test]
        public void Should_not_have_error_when_lastName_is_specified()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings());

            var model = new AddressModel();
            model.LastName = "Smith";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.LastName);
        }

        [Test]
        public void Should_have_error_when_company_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    CompanyEnabled = true,
                    CompanyRequired = true
                });
            model.Company = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Company);
            model.Company = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Company);


            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
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
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    CompanyEnabled = true
                });

            var model = new AddressModel();
            model.Company = "Company";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Company);
        }

        [Test]
        public void Should_have_error_when_streetaddress_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddressEnabled = true,
                    StreetAddressRequired = true
                });
            model.Address1 = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Address1);
            model.Address1 = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Address1);

            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddressEnabled = true,
                    StreetAddressRequired = false
                });
            model.Address1 = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Address1);
            model.Address1 = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Address1);
        }
        [Test]
        public void Should_not_have_error_when_streetaddress_is_specified()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddressEnabled = true
                });

            var model = new AddressModel();
            model.Address1 = "Street address";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Address1);
        }

        [Test]
        public void Should_have_error_when_streetaddress2_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddress2Enabled = true,
                    StreetAddress2Required = true
                });
            model.Address2 = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Address2);
            model.Address2 = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Address2);

            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddress2Enabled = true,
                    StreetAddress2Required = false
                });
            model.Address2 = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Address2);
            model.Address2 = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Address2);
        }
        [Test]
        public void Should_not_have_error_when_streetaddress2_is_specified()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddress2Enabled = true
                });

            var model = new AddressModel();
            model.Address2 = "Street address 2";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Address2);
        }

        [Test]
        public void Should_have_error_when_zippostalcode_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    ZipPostalCodeEnabled = true,
                    ZipPostalCodeRequired = true
                });
            model.ZipPostalCode = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ZipPostalCode);
            model.ZipPostalCode = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ZipPostalCode);


            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
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
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    StreetAddress2Enabled = true
                });

            var model = new AddressModel();
            model.ZipPostalCode = "zip";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ZipPostalCode);
        }

        [Test]
        public void Should_have_error_when_city_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    CityEnabled = true,
                    CityRequired = true
                });
            model.City = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.City);
            model.City = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.City);


            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
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
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    CityEnabled = true
                });

            var model = new AddressModel();
            model.City = "City";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.City);
        }

        [Test]
        public void Should_have_error_when_phone_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    PhoneEnabled = true,
                    PhoneRequired = true
                });
            model.PhoneNumber = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.PhoneNumber);
            model.PhoneNumber = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.PhoneNumber);

            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    PhoneEnabled = true,
                    PhoneRequired = false
                });
            model.PhoneNumber = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
            model.PhoneNumber = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
        }
        [Test]
        public void Should_not_have_error_when_phone_is_specified()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    PhoneEnabled = true
                });

            var model = new AddressModel();
            model.PhoneNumber = "Phone";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
        }

        [Test]
        public void Should_have_error_when_fax_is_null_or_empty_based_on_required_setting()
        {
            var model = new AddressModel();

            //required
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    FaxEnabled = true,
                    FaxRequired = true
                });
            model.FaxNumber = null;
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FaxNumber);
            model.FaxNumber = "";
            validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FaxNumber);


            //not required
            validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    FaxEnabled = true,
                    FaxRequired = false
                });
            model.FaxNumber = null;
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FaxNumber);
            model.FaxNumber = "";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FaxNumber);
        }
        [Test]
        public void Should_not_have_error_when_fax_is_specified()
        {
            var validator = new AddressValidator(_localizationService, _stateProvinceService,
                new AddressSettings
                {
                    FaxEnabled = true
                });

            var model = new AddressModel();
            model.FaxNumber = "Fax";
            validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FaxNumber);
        }
    }
}
