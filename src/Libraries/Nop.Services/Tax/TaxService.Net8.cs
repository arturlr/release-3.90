using System.Threading.Tasks;

namespace Nop.Services.Tax
{
    public class TaxService : ITaxService
    {
        public virtual async Task<decimal> GetTaxRateAsync(decimal price, int taxCategoryId, int customerId)
        {
            // TODO: Implement tax calculation
            return await Task.FromResult(0.0m);
        }

        public virtual async Task<decimal> GetProductPriceAsync(decimal price, bool includingTax)
        {
            // TODO: Calculate price with/without tax
            return await Task.FromResult(price);
        }
    }
}
