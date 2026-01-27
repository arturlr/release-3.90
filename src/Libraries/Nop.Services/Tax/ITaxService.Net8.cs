using System.Threading.Tasks;

namespace Nop.Services.Tax
{
    public interface ITaxService
    {
        Task<decimal> GetTaxRateAsync(decimal price, int taxCategoryId, int customerId);
        Task<decimal> GetProductPriceAsync(decimal price, bool includingTax);
    }
}
