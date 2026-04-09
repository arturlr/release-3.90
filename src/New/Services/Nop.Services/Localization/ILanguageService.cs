using Nop.Core.Domain.Localization;

namespace Nop.Services.Localization;

/// <summary>
/// Language service interface
/// </summary>
public interface ILanguageService
{
    Task DeleteLanguageAsync(Language language);
    Task<IList<Language>> GetAllLanguagesAsync(bool showHidden = false, int storeId = 0);
    Task<Language?> GetLanguageByIdAsync(int languageId);
    Task InsertLanguageAsync(Language language);
    Task UpdateLanguageAsync(Language language);
}
