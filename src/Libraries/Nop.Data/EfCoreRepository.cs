using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core;

namespace Nop.Data
{
    /// <summary>
    /// EF Core repository implementation
    /// </summary>
    public class EfCoreRepository<T> : IRepository<T> where T : BaseEntity
    {
        private readonly NopDbContext _context;
        private DbSet<T> _entities;

        public EfCoreRepository(NopDbContext context)
        {
            _context = context;
        }

        protected virtual DbSet<T> Entities => _entities ?? (_entities = _context.Set<T>());

        public virtual IQueryable<T> Table => Entities;

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await Entities.FindAsync(id);
        }

        public virtual async Task InsertAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await Entities.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Entities.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            Entities.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
