namespace Nop.Core.Domain.Directory
{

    public class ExchangeRate
    {

        public ExchangeRate()
        {
            CurrencyCode = string.Empty;
            Rate = 1.0m;
        }

        public string? CurrencyCode { get; set; }

        public decimal Rate { get; set; }

        public DateTime UpdatedOn { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1}", CurrencyCode, Rate);
        }
    }

}
