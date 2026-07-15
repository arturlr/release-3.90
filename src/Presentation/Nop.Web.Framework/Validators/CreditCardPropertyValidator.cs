using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Nop.Web.Framework.Validators
{
    public class CreditCardPropertyValidator<T, TProperty> : PropertyValidator<T, TProperty>
    {
        public override string Name => "CreditCardPropertyValidator";

        public override bool IsValid(ValidationContext<T> context, TProperty value)
        {
            var ccValue = value as string;
            if (String.IsNullOrWhiteSpace(ccValue))
                return false;

            ccValue = ccValue.Replace(" ", "");
            ccValue = ccValue.Replace("-", "");

            int checksum = 0;
            bool evenDigit = false;

            foreach (char digit in ccValue.Reverse())
            {
                if (!Char.IsDigit(digit))
                    return false;

                int digitValue = (digit - '0') * (evenDigit ? 2 : 1);
                evenDigit = !evenDigit;

                while (digitValue > 0)
                {
                    checksum += digitValue % 10;
                    digitValue /= 10;
                }
            }

            return (checksum % 10) == 0;
        }

        protected override string GetDefaultMessageTemplate(string errorCode)
        {
            return "Credit card number is not valid";
        }
    }
}
