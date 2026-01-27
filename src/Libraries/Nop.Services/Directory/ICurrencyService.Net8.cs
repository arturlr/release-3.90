using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Directory;

namespace Nop.Services.Directory
{
    public interface ICurrencyService
    {
        Task<Currency> GetCurrencyByIdAsync(int currencyId);
        Task<IList<Currency>> GetAllCurrenciesAsync();
        Task InsertCurrencyAsync(Currency currency);
        Task UpdateCurrencyAsync(Currency currency);
        Task DeleteCurrencyAsync(Currency currency);
    }
}
