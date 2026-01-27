using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nop.Core;

namespace Nop.Data
{
    /// <summary>
    /// EF Core database context interface
    /// </summary>
    public interface IDbContext
    {
        /// <summary>
        /// Get DbSet
        /// </summary>
        DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity;

        /// <summary>
        /// Save changes
        /// </summary>
        int SaveChanges();

        /// <summary>
        /// Execute stored procedure and load entities
        /// </summary>
        IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters)
            where TEntity : BaseEntity, new();

        /// <summary>
        /// Execute raw SQL query
        /// </summary>
        IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters);

        /// <summary>
        /// Execute SQL command
        /// </summary>
        int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters);
    }
}
