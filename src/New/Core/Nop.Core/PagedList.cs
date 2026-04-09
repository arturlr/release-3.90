namespace Nop.Core;

/// <summary>
/// Paged list
/// </summary>
public class PagedList<T> : List<T>, IPagedList<T>
{
    public PagedList(IQueryable<T> source, int pageIndex, int pageSize)
    {
        var total = source.Count();
        TotalCount = total;
        TotalPages = total / pageSize;
        if (total % pageSize > 0)
            TotalPages++;
        PageSize = pageSize;
        PageIndex = pageIndex;
        AddRange(source.Skip(pageIndex * pageSize).Take(pageSize).ToList());
    }

    public PagedList(IList<T> source, int pageIndex, int pageSize)
    {
        TotalCount = source.Count;
        TotalPages = TotalCount / pageSize;
        if (TotalCount % pageSize > 0)
            TotalPages++;
        PageSize = pageSize;
        PageIndex = pageIndex;
        AddRange(source.Skip(pageIndex * pageSize).Take(pageSize));
    }

    public PagedList(IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
    {
        TotalCount = totalCount;
        TotalPages = TotalCount / pageSize;
        if (TotalCount % pageSize > 0)
            TotalPages++;
        PageSize = pageSize;
        PageIndex = pageIndex;
        AddRange(source);
    }

    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex + 1 < TotalPages;
}
