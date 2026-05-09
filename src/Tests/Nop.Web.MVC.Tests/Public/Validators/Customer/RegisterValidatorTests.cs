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
    public class RegisterValidatorTests : BaseValidatorTests
    {
        private RegisterValidator _validator;
        private IStateProvinceService _stateProvinceService;
        private CustomerSettings _customerSettings;
        
        [SetUp]
        public new void Setup()
        {
            _customerSettings = new CustomerSettings();
            _stateProvinceService = new Mock<IStateProvinceService>().Object;
            _validator = new RegisterValidator(_localizationService, _stateProvinceService, _customerSettings);
        }
        
        [Test]
        public void Should_have_error_when_email_is_null_or_empty()
        {
            var model = new RegisterModel();
            model.Email = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
            model.Email = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_email_is_wrong_format()
        {
            var model = new RegisterModel();
            model.Email = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_not_have_error_when_email_is_correct_format()
        {
            var model = new RegisterModel();
            model.Email = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_firstName_is_null_or_empty()
        {
            var model = new RegisterModel();
            model.FirstName = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FirstName);
            model.FirstName = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FirstName);
        }

        [Test]
        public void Should_not_have_error_when_firstName_is_specified()
        {
            var model = new RegisterModel();
            model.FirstName = "John";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FirstName);
        }

        [Test]
        public void Should_have_error_when_lastName_is_null_or_empty()
        {
            var model = new RegisterModel();
            model.LastName = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.LastName);
            model.LastName = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.LastName);
        }

        [Test]
        public void Should_not_have_error_when_lastName_is_specified()
        {
            var model = new RegisterModel();
            model.LastName = "Smith";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.LastName);
        }

        [Test]
        public void Should_have_error_when_password_is_null_or_empty()
        {
            var model = new RegisterModel();
            model.Password = null;
            //we know that password should equal confirmation password
            model.ConfirmPassword = model.Password;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Password);
            model.Password = "";
            //we know that password should equal confirmation password
            model.ConfirmPassword = model.Password;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Test]
        public void Should_not_have_error_when_password_is_specified()
        {
            var model = new RegisterModel();
            model.Password = "password";
            //we know that password should equal confirmation password
            model.ConfirmPassword = model.Password;
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Test]
        public void Should_have_error_when_confirmPassword_is_null_or_empty()
        {
            var model = new RegisterModel();
            model.ConfirmPassword = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
            model.ConfirmPassword = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
        }

        [Test]
        public void Should_not_have_error_when_confirmPassword_is_specified()
        {
            var model = new RegisterModel();
            model.ConfirmPassword = "some password";
            //we know that new password should equal confirmation password
            model.Password = model.ConfirmPassword;
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
        }

        [Test]
        public void Should_have_error_when_password_doesnot_equal_confirmationPassword()
        {
            var model = new RegisterModel();
            model.Password = "some password";
            model.ConfirmPassword = "another password";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
        }

        [Test]
        public void Should_not_have_error_when_password_equals_confirmationPassword()
        {
            var model = new RegisterModel();
            model.Password = "some password";
            model.ConfirmPassword = "some password";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Password);
        }

        [Test]
        public void Should_validate_password_is_length()
        {
            _customerSettings.PasswordMinLength = 5;
            _validator = new RegisterValidator(_localizationService, _stateProvinceService, _customerSettings);

            var model = new RegisterModel();
            model.Password = "1234";
            //we know that password should equal confirmation password
            model.ConfirmPassword = model.Password;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Password);
            model.Password = "12345";
            //we know that password should equal confirmation password
            model.ConfirmPassword = model.Password;
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Password);
        }
    }
}
