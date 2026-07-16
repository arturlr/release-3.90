using FluentValidation.TestHelper;
using Nop.Core.Domain.Customers;
using Nop.Web.Models.Customer;
using Nop.Web.Validators.Customer;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Customer
{
    [TestFixture]
    public class PasswordRecoveryConfirmValidatorTests : BaseValidatorTests
    {
        private PasswordRecoveryConfirmValidator _validator;
        private CustomerSettings _customerSettings;
        
        [SetUp]
        public new void Setup()
        {
            _customerSettings = new CustomerSettings();
            _validator = new PasswordRecoveryConfirmValidator(_localizationService, _customerSettings);
        }

        [Test]
        public void Should_have_error_when_newPassword_is_null_or_empty()
        {
            var model = new PasswordRecoveryConfirmModel();
            model.NewPassword = null;
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.NewPassword);
            model.NewPassword = "";
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.NewPassword);
        }

        [Test]
        public void Should_not_have_error_when_newPassword_is_specified()
        {
            var model = new PasswordRecoveryConfirmModel();
            model.NewPassword = "new password";
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.NewPassword);
        }

        [Test]
        public void Should_have_error_when_confirmNewPassword_is_null_or_empty()
        {
            var model = new PasswordRecoveryConfirmModel();
            model.ConfirmNewPassword = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
            model.ConfirmNewPassword = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
        }

        [Test]
        public void Should_not_have_error_when_confirmNewPassword_is_specified()
        {
            var model = new PasswordRecoveryConfirmModel();
            model.ConfirmNewPassword = "some password";
            //we know that new password should equal confirmation password
            model.NewPassword = model.ConfirmNewPassword;
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.ConfirmNewPassword);
        }

        [Test]
        public void Should_have_error_when_newPassword_doesnot_equal_confirmationPassword()
        {
            var model = new PasswordRecoveryConfirmModel();
            model.NewPassword = "some password";
            model.ConfirmNewPassword = "another password";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
        }

        [Test]
        public void Should_not_have_error_when_newPassword_equals_confirmationPassword()
        {
            var model = new PasswordRecoveryConfirmModel();
            model.NewPassword = "some password";
            model.ConfirmNewPassword = "some password";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.NewPassword);
        }

        [Test]
        public void Should_validate_newPassword_is_length()
        {
            _customerSettings.PasswordMinLength = 5;
            _validator = new PasswordRecoveryConfirmValidator(_localizationService, _customerSettings);

            var model = new PasswordRecoveryConfirmModel();
            model.NewPassword = "1234";
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.NewPassword);
            model.NewPassword = "12345";
            //we know that new password should equal confirmation password
            model.ConfirmNewPassword = model.NewPassword;
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.NewPassword);
        }
    }
}
