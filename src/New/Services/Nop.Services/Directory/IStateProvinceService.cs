using Nop.Core.Domain.Directory;

namespace Nop.Services.Directory;

public interface IStateProvinceService
{
    Task DeleteStateProvinceAsync(StateProvince stateProvince);
    Task<StateProvince?> GetStateProvinceByIdAsync(int stateProvinceId);
    Task<StateProvince?> GetStateProvinceByAbbreviationAsync(string abbreviation);
    Task<IList<StateProvince>> GetStateProvincesByCountryIdAsync(int countryId, int languageId = 0, bool showHidden = false);
    Task<IList<StateProvince>> GetStateProvincesAsync(bool showHidden = false);
    Task InsertStateProvinceAsync(StateProvince stateProvince);
    Task UpdateStateProvinceAsync(StateProvince stateProvince);
}
