using FluentValidation;

namespace Nop.Web.Framework.Validators;

/// <summary>
/// Base validator for nopCommerce models. Provides PostInitialize hook for custom partial class extensions.
/// Legacy SetDatabaseValidationRules (System.Linq.Dynamic) dropped — use explicit RuleFor with MaximumLength instead.
/// </summary>
public abstract class BaseNopValidator<T> : AbstractValidator<T> where T : class
{
    protected BaseNopValidator()
    {
        PostInitialize();
    }

    /// <summary>
    /// Override in custom partial classes to add initialization code.
    /// </summary>
    protected virtual void PostInitialize()
    {
    }
}
