using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nop.Core;

namespace Nop.Data
{
    /// <summary>
    /// EF Core database context for nopCommerce
    /// </summary>
    public class NopDbContext : DbContext, IDbContext
    {
        public NopDbContext(DbContextOptions<NopDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply all entity configurations from this assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(NopDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        public new DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters)
            where TEntity : BaseEntity, new()
        {
            throw new NotImplementedException("Stored procedures will be implemented in Task 3");
        }

        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            throw new NotImplementedException("Raw SQL queries will be implemented in Task 3");
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            return Database.ExecuteSqlRaw(sql, parameters);
        }
    }
}
