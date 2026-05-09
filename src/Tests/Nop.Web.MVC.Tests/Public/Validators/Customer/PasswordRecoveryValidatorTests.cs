using FluentValidation.TestHelper;
using Nop.Web.Models.Customer;
using Nop.Web.Validators.Customer;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Customer
{
    [TestFixture]
    public class PasswordRecoveryValidatorTests : BaseValidatorTests
    {
        private PasswordRecoveryValidator _validator;
        
        [SetUp]
        public new void Setup()
        {
            _validator = new PasswordRecoveryValidator(_localizationService);
        }
        
        [Test]
        public void Should_have_error_when_email_is_null_or_empty()
        {
            var model = new PasswordRecoveryModel();
            model.Email = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
            model.Email = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_have_error_when_email_is_wrong_format()
        {
            var model = new PasswordRecoveryModel();
            model.Email = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Email);
        }

        [Test]
        public void Should_not_have_error_when_email_is_correct_format()
        {
            var model = new PasswordRecoveryModel();
            model.Email = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Email);
        }
    }
}
