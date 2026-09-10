using System.Linq;
using System.Linq.Dynamic.Core;
using FluentValidation;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Localization;

namespace Nop.Web.Framework.Validators
{
    /// <summary>
    /// Task 6.2: <c>System.Linq.Dynamic</c> -&gt; <c>System.Linq.Dynamic.Core</c>, and
    /// <c>DynamicExpression.ParseLambda&lt;T, TResult&gt;(expression, values)</c> -&gt;
    /// <c>DynamicExpressionParser.ParseLambda&lt;T, TResult&gt;(ParsingConfig, createParameterCtor,
    /// expression)</c> - the static entry point was renamed AND the generic two-type-argument
    /// overload now requires an explicit <c>ParsingConfig</c> plus the <c>createParameterCtor</c>
    /// flag that the legacy API applied implicitly. Same return type
    /// (<c>Expression&lt;Func&lt;T, TResult&gt;&gt;</c>), same expression language, so the
    /// <c>RuleFor(...)</c> calls below are unchanged.
    /// See the note on <c>Kendoui.QueryableExtensions</c> for why the old <c>using</c> compiled
    /// at declaration time yet still had to change.
    /// </summary>
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
        /// Sets validation rule(s) to appropriate database model
        /// </summary>
        /// <typeparam name="TObject">Object type</typeparam>
        /// <param name="dbContext">Database context</param>
        /// <param name="filterStringPropertyNames">Properties to skip</param>
        protected virtual void SetDatabaseValidationRules<TObject>(IDbContext dbContext, params string[] filterStringPropertyNames)
        {
            SetStringPropertiesMaxLength<TObject>(dbContext, filterStringPropertyNames);
            SetDecimalMaxValue<TObject>(dbContext);
        }

        /// <summary>
        /// Sets length validation rule(s) to string properties according to appropriate database model
        /// </summary>
        /// <typeparam name="TObject">Object type</typeparam>
        /// <param name="dbContext">Database context</param>
        /// <param name="filterPropertyNames">Properties to skip</param>
        protected virtual void SetStringPropertiesMaxLength<TObject>(IDbContext dbContext, params string[] filterPropertyNames)
        {
            if (dbContext == null)
                return;

            var dbObjectType = typeof(TObject);

            var names = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(string) && !filterPropertyNames.Contains(p.Name))
                .Select(p => p.Name).ToArray();

            var maxLength = dbContext.GetColumnsMaxLength(dbObjectType.Name, names);
            var expression = maxLength.Keys.ToDictionary(name => name, name => DynamicExpressionParser.ParseLambda<T, string>(ParsingConfig.Default, true, name));

            foreach (var expr in expression)
            {
                RuleFor(expr.Value).Length(0, maxLength[expr.Key]);
            }
        }

        /// <summary>
        /// Sets max value validation rule(s) to decimal properties according to appropriate database model
        /// </summary>
        /// <typeparam name="TObject">Object type</typeparam>
        /// <param name="dbContext">Database context</param>
        protected virtual void SetDecimalMaxValue<TObject>(IDbContext dbContext)
        {
            var localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            if (dbContext == null)
                return;

            var dbObjectType = typeof(TObject);

            var names = typeof(T).GetProperties()
                .Where(p => p.PropertyType == typeof(decimal))
                .Select(p => p.Name).ToArray();

            var maxValues = dbContext.GetDecimalMaxValue(dbObjectType.Name, names);
            var expression = maxValues.Keys.ToDictionary(name => name, name => DynamicExpressionParser.ParseLambda<T, decimal>(ParsingConfig.Default, true, name));

            foreach (var expr in expression)
            {
                RuleFor(expr.Value).IsDecimal(maxValues[expr.Key]).WithMessage(string.Format(localizationService.GetResource("Nop.Web.Framework.Validators.MaxDecimal"), maxValues[expr.Key] - 1));
            }
        }
    }
}