using Nop.Core;
using Nop.Core.Data;

namespace Nop.Tests;

/// <summary>
/// In-memory IRepository implementation for unit tests.
/// Uses a simple List instead of EF Core — no database needed.
/// </summary>
public class FakeRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly List<T> _store = [];
    private int _nextId = 1;

    public T? GetById(int id) => _store.FirstOrDefault(e => e.Id == id);

    public void Insert(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (entity.Id == 0)
            entity.Id = _nextId++;
        _store.Add(entity);
    }

    public void Insert(IEnumerable<T> entities)
    {
        foreach (var e in entities)
            Insert(e);
    }

    public void Update(T entity) { /* entity is already in the list by reference */ }

    public void Update(IEnumerable<T> entities) { }

    public void Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _store.Remove(entity);
    }

    public void Delete(IEnumerable<T> entities)
    {
        foreach (var e in entities.ToList())
            _store.Remove(e);
    }

    public IQueryable<T> Table => _store.AsQueryable();
    public IQueryable<T> TableNoTracking => _store.AsQueryable();
}
