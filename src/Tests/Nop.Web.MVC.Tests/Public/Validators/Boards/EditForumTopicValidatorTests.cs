using FluentValidation.TestHelper;
using Nop.Web.Models.Boards;
using Nop.Web.Validators.Boards;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Boards
{
    [TestFixture]
    public class EditForumTopicValidatorTests : BaseValidatorTests
    {
        private EditForumTopicValidator _validator;
        
        [SetUp]
        public new void Setup()
        {
            _validator = new EditForumTopicValidator(_localizationService);
        }

        [Test]
        public void Should_have_error_when_subject_is_null_or_empty()
        {
            var model = new EditForumTopicModel();
            model.Subject = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Subject);
            model.Subject = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Subject);
        }

        [Test]
        public void Should_not_have_error_when_subject_is_specified()
        {
            var model = new EditForumTopicModel();
            model.Subject = "some comment";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Subject);
        }

        [Test]
        public void Should_have_error_when_text_is_null_or_empty()
        {
            var model = new EditForumTopicModel();
            model.Text = null;
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Text);
            model.Text = "";
            _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Text);
        }

        [Test]
        public void Should_not_have_error_when_text_is_specified()
        {
            var model = new EditForumTopicModel();
            model.Text = "some comment";
            _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.Text);
        }
    }
}
