using FluentValidation;
using FluentValidation.Validators;
using Nop.Services.Catalog;

namespace Nop.Web.Framework.Validators
{
    public class DecimalPropertyValidator<T, TProperty> : PropertyValidator<T, TProperty>
    {
        private readonly decimal _maxValue;

        public override string Name => "DecimalPropertyValidator";

        public DecimalPropertyValidator(decimal maxValue)
        {
            this._maxValue = maxValue;
        }

        public override bool IsValid(ValidationContext<T> context, TProperty value)
        {
            decimal decimalValue;
            if (decimal.TryParse(value?.ToString(), out decimalValue))
            {
                return RoundingHelper.RoundPrice(decimalValue) < _maxValue;
            }
            return false;
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "Decimal value is out of range";
        }
    }
}
