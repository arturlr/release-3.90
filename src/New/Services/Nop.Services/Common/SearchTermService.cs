using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Common;
using Nop.Services.Events;

namespace Nop.Services.Common;

public class SearchTermService : ISearchTermService
{
    private readonly IRepository<SearchTerm> _searchTermRepository;
    private readonly IEventPublisher _eventPublisher;

    public SearchTermService(
        IRepository<SearchTerm> searchTermRepository,
        IEventPublisher eventPublisher)
    {
        _searchTermRepository = searchTermRepository;
        _eventPublisher = eventPublisher;
    }

    public virtual Task<SearchTerm?> GetSearchTermByIdAsync(int searchTermId)
    {
        if (searchTermId == 0)
            return Task.FromResult<SearchTerm?>(null);
        return Task.FromResult(_searchTermRepository.GetById(searchTermId));
    }

    public virtual Task<SearchTerm?> GetSearchTermByKeywordAsync(string keyword, int storeId)
    {
        if (string.IsNullOrEmpty(keyword))
            return Task.FromResult<SearchTerm?>(null);

        var searchTerm = _searchTermRepository.TableNoTracking
            .Where(st => st.Keyword == keyword && st.StoreId == storeId)
            .OrderBy(st => st.Id)
            .FirstOrDefault();

        return Task.FromResult(searchTerm);
    }

    public virtual Task<IPagedList<SearchTermReportLine>> GetStatsAsync(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _searchTermRepository.TableNoTracking
            .GroupBy(st => st.Keyword)
            .Select(g => new SearchTermReportLine
            {
                Keyword = g.Key,
                Count = g.Sum(o => o.Count)
            })
            .OrderByDescending(m => m.Count);

        var result = new PagedList<SearchTermReportLine>(query, pageIndex, pageSize);
        return Task.FromResult<IPagedList<SearchTermReportLine>>(result);
    }

    public virtual async Task InsertSearchTermAsync(SearchTerm searchTerm)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        _searchTermRepository.Insert(searchTerm);
        await _eventPublisher.EntityInsertedAsync(searchTerm);
    }

    public virtual async Task UpdateSearchTermAsync(SearchTerm searchTerm)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        _searchTermRepository.Update(searchTerm);
        await _eventPublisher.EntityUpdatedAsync(searchTerm);
    }

    public virtual async Task DeleteSearchTermAsync(SearchTerm searchTerm)
    {
        ArgumentNullException.ThrowIfNull(searchTerm);
        _searchTermRepository.Delete(searchTerm);
        await _eventPublisher.EntityDeletedAsync(searchTerm);
    }
}
