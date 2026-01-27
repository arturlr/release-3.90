using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Localization;
using Nop.Data;

namespace Nop.Services.Localization
{
    public class LanguageService : ILanguageService
    {
        private readonly IRepository<Language> _languageRepository;

        public LanguageService(IRepository<Language> languageRepository)
        {
            _languageRepository = languageRepository;
        }

        public virtual async Task<Language> GetLanguageByIdAsync(int languageId)
        {
            if (languageId == 0)
                return null;

            return await _languageRepository.GetByIdAsync(languageId);
        }

        public virtual async Task<IList<Language>> GetAllLanguagesAsync()
        {
            var query = _languageRepository.Table
                .Where(l => l.Published)
                .OrderBy(l => l.DisplayOrder);

            return await query.ToListAsync();
        }

        public virtual async Task InsertLanguageAsync(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            await _languageRepository.InsertAsync(language);
        }

        public virtual async Task UpdateLanguageAsync(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            await _languageRepository.UpdateAsync(language);
        }

        public virtual async Task DeleteLanguageAsync(Language language)
        {
            if (language == null)
                throw new ArgumentNullException(nameof(language));

            await _languageRepository.DeleteAsync(language);
        }
    }
}
