using FluentValidation;
using FluentValidation.Validators;
using Nop.Services.Catalog;

namespace Nop.Web.Framework.Validators
{
    public class DecimalPropertyValidator<T, TProperty> : PropertyValidator<T, TProperty>
    {
        private readonly decimal _maxValue;

        public DecimalPropertyValidator(decimal maxValue)
        {
            this._maxValue = maxValue;
        }

        public override string Name => "DecimalPropertyValidator";

        public override bool IsValid(ValidationContext<T> context, TProperty value)
        {
            if (value == null) return false;
            if (decimal.TryParse(value.ToString(), out decimal decimalValue))
            {
                return RoundingHelper.RoundPrice(decimalValue) < _maxValue;
            }
            return false;
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
            => "Decimal value is out of range";
    }
}
