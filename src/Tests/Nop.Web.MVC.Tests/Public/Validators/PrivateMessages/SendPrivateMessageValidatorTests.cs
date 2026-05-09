using FluentValidation.TestHelper;
using Nop.Web.Models.PrivateMessages;
using Nop.Web.Validators.PrivateMessages;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.PrivateMessages
{
    [TestFixture]
    public class SendPrivateMessageValidatorTests : BaseValidatorTests
    {
        private SendPrivateMessageValidator _validator;
        
        [SetUp]
        public new void Setup()
        {
            _validator = new SendPrivateMessageValidator(_localizationService);
        }

        [Test]
        public void Should_have_error_when_subject_is_null_or_empty()
        {
            var model = new SendPrivateMessageModel();
            model.Subject = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Subject);
            model.Subject = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Subject);
        }

        [Test]
        public void Should_not_have_error_when_subject_is_specified()
        {
            var model = new SendPrivateMessageModel();
            model.Subject = "some comment";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Subject);
        }

        [Test]
        public void Should_have_error_when_message_is_null_or_empty()
        {
            var model = new SendPrivateMessageModel();
            model.Message = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Message);
            model.Message = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Message);
        }

        [Test]
        public void Should_not_have_error_when_message_is_specified()
        {
            var model = new SendPrivateMessageModel();
            model.Message = "some comment";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Message);
        }
    }
}
