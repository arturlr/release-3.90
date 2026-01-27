using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Directory;

namespace Nop.Services.Directory
{
    public interface ICountryService
    {
        Task<Country> GetCountryByIdAsync(int countryId);
        Task<IList<Country>> GetAllCountriesAsync();
        Task<IList<StateProvince>> GetStateProvincesByCountryIdAsync(int countryId);
        Task InsertCountryAsync(Country country);
        Task UpdateCountryAsync(Country country);
        Task DeleteCountryAsync(Country country);
    }
}
