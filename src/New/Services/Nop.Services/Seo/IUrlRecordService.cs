using Nop.Core;
using Nop.Core.Domain.Seo;

namespace Nop.Services.Seo;

public interface IUrlRecordService
{
    Task DeleteUrlRecordAsync(UrlRecord urlRecord);
    Task DeleteUrlRecordsAsync(IList<UrlRecord> urlRecords);
    Task<UrlRecord?> GetUrlRecordByIdAsync(int urlRecordId);
    Task InsertUrlRecordAsync(UrlRecord urlRecord);
    Task UpdateUrlRecordAsync(UrlRecord urlRecord);
    Task<UrlRecord?> GetBySlugAsync(string slug);
    Task<IPagedList<UrlRecord>> GetAllUrlRecordsAsync(string slug = "", int pageIndex = 0, int pageSize = int.MaxValue);
    Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId);
    Task SaveSlugAsync<T>(T entity, string slug, int languageId) where T : BaseEntity, ISlugSupported;
}
