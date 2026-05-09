using System;
using System.Linq;
using FluentValidation;
using FluentValidation.Validators;

namespace Nop.Web.Framework.Validators
{
    public class CreditCardPropertyValidator<T> : PropertyValidator<T, string>
    {
        public override string Name => "CreditCardPropertyValidator";

        public override bool IsValid(ValidationContext<T> context, string ccValue)
        {
            if (String.IsNullOrWhiteSpace(ccValue))
                return false;

            ccValue = ccValue.Replace(" ", "");
            ccValue = ccValue.Replace("-", "");

            int checksum = 0;
            bool evenDigit = false;

            //http://www.beachnet.com/~hstiles/cardtype.html
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
            => "Credit card number is not valid";
    }
}
