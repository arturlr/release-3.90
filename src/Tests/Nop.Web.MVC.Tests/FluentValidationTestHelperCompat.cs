using System;
using System.Linq.Expressions;
using FluentValidation;
using FluentValidation.TestHelper;

namespace Nop.Web.MVC.Tests
{
    /// <summary>
    /// Compatibility shim: bridges old FluentValidation test API to the new TestValidate API.
    /// Old: validator.ShouldHaveValidationErrorFor(x => x.Prop, model)
    /// New: validator.TestValidate(model).ShouldHaveValidationErrorFor(x => x.Prop)
    /// </summary>
    public static class FluentValidationTestHelperCompat
    {
        public static ITestValidationWith ShouldHaveValidationErrorFor<T, TProperty>(
            this IValidator<T> validator,
            Expression<Func<T, TProperty>> expression,
            T model) where T : class
        {
            var result = validator.TestValidate(model);
            return result.ShouldHaveValidationErrorFor(expression);
        }

        public static void ShouldNotHaveValidationErrorFor<T, TProperty>(
            this IValidator<T> validator,
            Expression<Func<T, TProperty>> expression,
            T model) where T : class
        {
            var result = validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(expression);
        }
    }
}
