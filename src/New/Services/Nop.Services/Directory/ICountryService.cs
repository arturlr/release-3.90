using Nop.Core.Domain.Directory;

namespace Nop.Services.Directory;

public interface ICountryService
{
    Task DeleteCountryAsync(Country country);
    Task<IList<Country>> GetAllCountriesAsync(int languageId = 0, bool showHidden = false);
    Task<IList<Country>> GetAllCountriesForBillingAsync(int languageId = 0, bool showHidden = false);
    Task<IList<Country>> GetAllCountriesForShippingAsync(int languageId = 0, bool showHidden = false);
    Task<Country?> GetCountryByIdAsync(int countryId);
    Task<IList<Country>> GetCountriesByIdsAsync(int[] countryIds);
    Task<Country?> GetCountryByTwoLetterIsoCodeAsync(string twoLetterIsoCode);
    Task<Country?> GetCountryByThreeLetterIsoCodeAsync(string threeLetterIsoCode);
    Task InsertCountryAsync(Country country);
    Task UpdateCountryAsync(Country country);
}
