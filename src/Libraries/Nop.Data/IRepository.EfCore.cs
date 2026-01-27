using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;

namespace Nop.Data
{
    /// <summary>
    /// Repository interface
    /// </summary>
    public interface IRepository<T> where T : BaseEntity
    {
        /// <summary>
        /// Gets a table
        /// </summary>
        IQueryable<T> Table { get; }

        /// <summary>
        /// Gets an entity by ID
        /// </summary>
        Task<T> GetByIdAsync(int id);

        /// <summary>
        /// Inserts an entity
        /// </summary>
        Task InsertAsync(T entity);

        /// <summary>
        /// Updates an entity
        /// </summary>
        Task UpdateAsync(T entity);

        /// <summary>
        /// Deletes an entity
        /// </summary>
        Task DeleteAsync(T entity);
    }
}
