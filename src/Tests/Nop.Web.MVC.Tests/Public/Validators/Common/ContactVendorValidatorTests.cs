using FluentValidation.TestHelper;
using Nop.Core.Domain.Common;
using Nop.Web.Models.Common;
using Nop.Web.Validators.Common;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Common
{
    [TestFixture]
    public class ContactVendorValidatorTests : BaseValidatorTests
    {
        private ContactVendorValidator _validator;
        private CommonSettings _commonSettings;
        
        [SetUp]
        public new void Setup()
        {
            _commonSettings = new CommonSettings();
            _validator = new ContactVendorValidator(_localizationService, _commonSettings);
        }
        
        [Test]
        public void Should_have_error_when_email_is_null_or_empty()
        {
            var model = new ContactVendorModel();
            model.Email = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
            model.Email = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_email_is_wrong_format()
        {
            var model = new ContactVendorModel();
            model.Email = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_not_have_error_when_email_is_correct_format()
        {
            var model = new ContactVendorModel();
            model.Email = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_fullName_is_null_or_empty()
        {
            var model = new ContactVendorModel();
            model.FullName = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FullName);
            model.FullName = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FullName);
        }

        [Test]
        public void Should_not_have_error_when_fullName_is_specified()
        {
            var model = new ContactVendorModel();
            model.FullName = "John Smith";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FullName);
        }

        [Test]
        public void Should_have_error_when_enquiry_is_null_or_empty()
        {
            var model = new ContactVendorModel();
            model.Enquiry = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Enquiry);
            model.Enquiry = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Enquiry);
        }

        [Test]
        public void Should_not_have_error_when_enquiry_is_specified()
        {
            var model = new ContactVendorModel();
            model.Enquiry = "please call me back";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Enquiry);
        }
    }
}
