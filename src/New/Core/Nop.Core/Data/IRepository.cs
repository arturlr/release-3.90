namespace Nop.Core.Data;

/// <summary>
/// Repository
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    T? GetById(int id);
    void Insert(T entity);
    void Insert(IEnumerable<T> entities);
    void Update(T entity);
    void Update(IEnumerable<T> entities);
    void Delete(T entity);
    void Delete(IEnumerable<T> entities);
    IQueryable<T> Table { get; }
    IQueryable<T> TableNoTracking { get; }
}
