using FluentValidation.TestHelper;
using Nop.Web.Models.Catalog;
using Nop.Web.Validators.Catalog;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Catalog
{
    [TestFixture]
    public class ProductEmailAFriendValidatorTests : BaseValidatorTests
    {
        private ProductEmailAFriendValidator _validator;
        
        [SetUp]
        public new void Setup()
        {
            _validator = new ProductEmailAFriendValidator(_localizationService);
        }
        
        [Test]
        public void Should_have_error_when_friendEmail_is_null_or_empty()
        {
            var model = new ProductEmailAFriendModel();
            model.FriendEmail = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FriendEmail);
            model.FriendEmail = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FriendEmail);
        }

        [Test]
        public void Should_have_error_when_friendEmail_is_wrong_format()
        {
            var model = new ProductEmailAFriendModel();
            model.FriendEmail = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FriendEmail);
        }

        [Test]
        public void Should_not_have_error_when_friendEmail_is_correct_format()
        {
            var model = new ProductEmailAFriendModel();
            model.FriendEmail = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FriendEmail);
        }

        [Test]
        public void Should_have_error_when_yourEmailAddress_is_null_or_empty()
        {
            var model = new ProductEmailAFriendModel();
            model.YourEmailAddress = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.YourEmailAddress);
            model.YourEmailAddress = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.YourEmailAddress);
        }

        [Test]
        public void Should_have_error_when_yourEmailAddress_is_wrong_format()
        {
            var model = new ProductEmailAFriendModel();
            model.YourEmailAddress = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.YourEmailAddress);
        }

        [Test]
        public void Should_not_have_error_when_yourEmailAddress_is_correct_format()
        {
            var model = new ProductEmailAFriendModel();
            model.YourEmailAddress = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.YourEmailAddress);
        }
    }
}
