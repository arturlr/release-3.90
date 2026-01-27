using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Directory;
using Nop.Data;

namespace Nop.Services.Directory
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IRepository<Currency> _currencyRepository;

        public CurrencyService(IRepository<Currency> currencyRepository)
        {
            _currencyRepository = currencyRepository;
        }

        public virtual async Task<Currency> GetCurrencyByIdAsync(int currencyId)
        {
            if (currencyId == 0)
                return null;

            return await _currencyRepository.GetByIdAsync(currencyId);
        }

        public virtual async Task<IList<Currency>> GetAllCurrenciesAsync()
        {
            var query = _currencyRepository.Table
                .Where(c => c.Published)
                .OrderBy(c => c.DisplayOrder);

            return await query.ToListAsync();
        }

        public virtual async Task InsertCurrencyAsync(Currency currency)
        {
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            await _currencyRepository.InsertAsync(currency);
        }

        public virtual async Task UpdateCurrencyAsync(Currency currency)
        {
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            await _currencyRepository.UpdateAsync(currency);
        }

        public virtual async Task DeleteCurrencyAsync(Currency currency)
        {
            if (currency == null)
                throw new ArgumentNullException(nameof(currency));

            await _currencyRepository.DeleteAsync(currency);
        }
    }
}
