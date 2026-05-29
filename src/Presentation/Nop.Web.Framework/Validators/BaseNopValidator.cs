using FluentValidation;

namespace Nop.Web.Framework.Validators
{
    public abstract class BaseNopValidator<T> : AbstractValidator<T> where T : class
    {
        protected BaseNopValidator()
        {
            PostInitialize();
        }

        /// <summary>
        /// Developers can override this method in custom partial classes
        /// in order to add some custom initialization code to constructors
        /// </summary>
        protected virtual void PostInitialize()
        {
        }

        /// <summary>
        /// Sets validation rule(s) to appropriate database model.
        /// Stub - database validation rules require EF Core metadata APIs.
        /// </summary>
        protected virtual void SetDatabaseValidationRules<TObject>(object dbContext, params string[] filterStringPropertyNames)
        {
            // TODO: Implement using EF Core metadata APIs
            // In EF Core, you'd use IModel to get entity type metadata
        }
    }
}
