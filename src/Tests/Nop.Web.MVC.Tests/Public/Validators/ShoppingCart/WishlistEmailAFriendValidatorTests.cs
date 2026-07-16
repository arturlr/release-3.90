using FluentValidation.TestHelper;
using Nop.Web.Models.ShoppingCart;
using Nop.Web.Validators.ShoppingCart;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.ShoppingCart
{
    [TestFixture]
    public class WishlistEmailAFriendValidatorTests : BaseValidatorTests
    {
        private WishlistEmailAFriendValidator _validator;
        
        [SetUp]
        public new void Setup()
        {
            _validator = new WishlistEmailAFriendValidator(_localizationService);
        }
        
        [Test]
        public void Should_have_error_when_friendEmail_is_null_or_empty()
        {
            var model = new WishlistEmailAFriendModel();
            model.FriendEmail = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FriendEmail);
            model.FriendEmail = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FriendEmail);
        }

        [Test]
        public void Should_have_error_when_friendEmail_is_wrong_format()
        {
            var model = new WishlistEmailAFriendModel();
            model.FriendEmail = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.FriendEmail);
        }

        [Test]
        public void Should_not_have_error_when_friendEmail_is_correct_format()
        {
            var model = new WishlistEmailAFriendModel();
            model.FriendEmail = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.FriendEmail);
        }

        [Test]
        public void Should_have_error_when_yourEmailAddress_is_null_or_empty()
        {
            var model = new WishlistEmailAFriendModel();
            model.YourEmailAddress = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.YourEmailAddress);
            model.YourEmailAddress = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.YourEmailAddress);
        }

        [Test]
        public void Should_have_error_when_yourEmailAddress_is_wrong_format()
        {
            var model = new WishlistEmailAFriendModel();
            model.YourEmailAddress = "adminexample.com";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.YourEmailAddress);
        }

        [Test]
        public void Should_not_have_error_when_yourEmailAddress_is_correct_format()
        {
            var model = new WishlistEmailAFriendModel();
            model.YourEmailAddress = "admin@example.com";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.YourEmailAddress);
        }
    }
}
