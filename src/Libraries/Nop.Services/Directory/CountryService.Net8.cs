using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Directory;
using Nop.Data;

namespace Nop.Services.Directory
{
    public class CountryService : ICountryService
    {
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<StateProvince> _stateProvinceRepository;

        public CountryService(
            IRepository<Country> countryRepository,
            IRepository<StateProvince> stateProvinceRepository)
        {
            _countryRepository = countryRepository;
            _stateProvinceRepository = stateProvinceRepository;
        }

        public virtual async Task<Country> GetCountryByIdAsync(int countryId)
        {
            if (countryId == 0)
                return null;

            return await _countryRepository.GetByIdAsync(countryId);
        }

        public virtual async Task<IList<Country>> GetAllCountriesAsync()
        {
            var query = _countryRepository.Table
                .Where(c => c.Published)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name);

            return await query.ToListAsync();
        }

        public virtual async Task<IList<StateProvince>> GetStateProvincesByCountryIdAsync(int countryId)
        {
            var query = _stateProvinceRepository.Table
                .Where(s => s.CountryId == countryId && s.Published)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name);

            return await query.ToListAsync();
        }

        public virtual async Task InsertCountryAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            await _countryRepository.InsertAsync(country);
        }

        public virtual async Task UpdateCountryAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            await _countryRepository.UpdateAsync(country);
        }

        public virtual async Task DeleteCountryAsync(Country country)
        {
            if (country == null)
                throw new ArgumentNullException(nameof(country));

            await _countryRepository.DeleteAsync(country);
        }
    }
}
