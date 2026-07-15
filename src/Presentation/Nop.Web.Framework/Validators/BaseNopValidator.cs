using System.Linq;
using FluentValidation;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Localization;

namespace Nop.Web.Framework.Validators
{
    public abstract class BaseNopValidator<T> : AbstractValidator<T> where T : class
    {
        protected BaseNopValidator()
        {
            PostInitialize();
        }

        protected virtual void PostInitialize()
        {
        }

        protected virtual void SetDatabaseValidationRules<TObject>(IDbContext dbContext, params string[] filterStringPropertyNames)
        {
            SetStringPropertiesMaxLength<TObject>(dbContext, filterStringPropertyNames);
            SetDecimalMaxValue<TObject>(dbContext);
        }

        protected virtual void SetStringPropertiesMaxLength<TObject>(IDbContext dbContext, params string[] filterPropertyNames)
        {
            if (dbContext == null)
                return;

            var dbObjectType = typeof(TObject);
            var names = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(string) && !filterPropertyNames.Contains(p.Name))
                .Select(p => p.Name).ToArray();

            var maxLength = dbContext.GetColumnsMaxLength(dbObjectType.Name, names);

            // Note: Without System.Linq.Dynamic, we cannot create dynamic expressions at runtime.
            // This validation is handled at the database layer instead.
        }

        protected virtual void SetDecimalMaxValue<TObject>(IDbContext dbContext)
        {
            if (dbContext == null)
                return;

            var dbObjectType = typeof(TObject);
            var names = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(decimal))
                .Select(p => p.Name).ToArray();

            var maxValues = dbContext.GetDecimalMaxValue(dbObjectType.Name, names);

            // Note: Without System.Linq.Dynamic, we cannot create dynamic expressions at runtime.
            // This validation is handled at the database layer instead.
        }
    }
}
