using Nop.Web.Validators.Catalog;
using NUnit.Framework;

namespace Nop.Web.MVC.Tests.Public.Validators.Catalog
{
    [TestFixture]
    public class ProductReviewsValidatorTests : BaseValidatorTests
    {
        private ProductReviewsValidator _validator;

        [SetUp]
        public new void Setup()
        {
            _validator = new ProductReviewsValidator(_localizationService);
        }

        //TODO uncomment tests when the following FluentVlidation issue is fixed http://fluentvalidation.codeplex.com/workitem/7095

        //[Test]
        //public void Should_have_error_when_title_is_null_or_empty()
        //{
        //    var model = new ProductReviewsModel();
        //    model.AddProductReview.Title = null;
        //    _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.AddProductReview.Title);
        //    model.AddProductReview.Title = "";
        //    _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.AddProductReview.Title);
        //}

        //[Test]
        //public void Should_not_have_error_when_title_is_specified()
        //{
        //    var model = new ProductReviewsModel();
        //    model.AddProductReview.Title = "some comment";
        //    _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.AddProductReview.Title);
        //}

        //[Test]
        //public void Should_have_error_when_reviewText_is_null_or_empty()
        //{
        //    var model = new ProductReviewsModel();
        //    model.AddProductReview.ReviewText = null;
        //    _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.AddProductReview.ReviewText);
        //    model.AddProductReview.ReviewText = "";
        //    _validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.AddProductReview.ReviewText);
        //}

        //[Test]
        //public void Should_not_have_error_when_reviewText_is_specified()
        //{
        //    var model = new ProductReviewsModel();
        //    model.AddProductReview.ReviewText = "some comment";
        //    _validator.TestValidate(model).ShouldNotHaveValidationErrorFor(x => x.AddProductReview.ReviewText);
        //}
    }
}
