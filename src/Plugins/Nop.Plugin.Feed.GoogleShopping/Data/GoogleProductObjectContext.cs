using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    public class GoogleProductObjectContext : DbContext, IDbContext
    {
        public GoogleProductObjectContext(DbContextOptions<GoogleProductObjectContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new GoogleProductRecordMap());
            base.OnModelCreating(modelBuilder);
        }

        public new DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        public void Install()
        {
            Database.EnsureCreated();
        }

        public void Uninstall()
        {
            this.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS [GoogleProduct]");
        }

        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            throw new NotImplementedException();
        }

        public IList<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            return this.Database.ExecuteSqlRaw(sql, parameters);
        }

        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            Entry(entity).State = EntityState.Detached;
        }

        public bool AutoDetectChangesEnabled
        {
            get { return ChangeTracker.AutoDetectChangesEnabled; }
            set { ChangeTracker.AutoDetectChangesEnabled = value; }
        }
    }
}
