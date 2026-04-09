using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Core.Data;

namespace Nop.Data;

public class EfRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly NopDbContext _context;
    private DbSet<T>? _entities;

    public EfRepository(NopDbContext context)
    {
        _context = context;
    }

    private DbSet<T> Entities => _entities ??= _context.Set<T>();

    public T? GetById(int id)
    {
        return Entities.Find(id);
    }

    public void Insert(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Add(entity);
        _context.SaveChanges();
    }

    public void Insert(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.AddRange(entities);
        _context.SaveChanges();
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _context.SaveChanges();
    }

    public void Update(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        _context.SaveChanges();
    }

    public void Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Entities.Remove(entity);
        _context.SaveChanges();
    }

    public void Delete(IEnumerable<T> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Entities.RemoveRange(entities);
        _context.SaveChanges();
    }

    public IQueryable<T> Table => Entities;

    public IQueryable<T> TableNoTracking => Entities.AsNoTracking();
}
