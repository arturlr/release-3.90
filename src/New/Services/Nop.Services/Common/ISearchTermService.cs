using Nop.Core;
using Nop.Core.Domain.Common;

namespace Nop.Services.Common;

public interface ISearchTermService
{
    Task<SearchTerm?> GetSearchTermByIdAsync(int searchTermId);
    Task<SearchTerm?> GetSearchTermByKeywordAsync(string keyword, int storeId);
    Task<IPagedList<SearchTermReportLine>> GetStatsAsync(int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertSearchTermAsync(SearchTerm searchTerm);
    Task UpdateSearchTermAsync(SearchTerm searchTerm);
    Task DeleteSearchTermAsync(SearchTerm searchTerm);
}
