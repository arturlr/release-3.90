using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Localization;

namespace Nop.Services.Localization
{
    public interface ILanguageService
    {
        Task<Language> GetLanguageByIdAsync(int languageId);
        Task<IList<Language>> GetAllLanguagesAsync();
        Task InsertLanguageAsync(Language language);
        Task UpdateLanguageAsync(Language language);
        Task DeleteLanguageAsync(Language language);
    }
}
