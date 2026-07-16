using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Pickup.PickupInStore.Domain;

namespace Nop.Plugin.Pickup.PickupInStore.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    public class StorePickupPointObjectContext : DbContext, IDbContext
    {
        #region Ctor

        public StorePickupPointObjectContext(DbContextOptions<StorePickupPointObjectContext> options)
            : base(options)
        {
        }

        public StorePickupPointObjectContext(string connectionString)
            : base(GetOptions(connectionString))
        {
        }

        private static DbContextOptions<StorePickupPointObjectContext> GetOptions(string connectionString)
        {
            return new DbContextOptionsBuilder<StorePickupPointObjectContext>()
                .UseSqlServer(connectionString)
                .Options;
        }

        #endregion

        #region Utilities

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new StorePickupPointMap());
            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region Methods

        public string CreateDatabaseScript()
        {
            return Database.GenerateCreateScript();
        }

        public new DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        /// <summary>
        /// Install
        /// </summary>
        public void Install()
        {
            var dbScript = CreateDatabaseScript();
            Database.ExecuteSqlRaw(dbScript);
            SaveChanges();
        }

        /// <summary>
        /// Uninstall
        /// </summary>
        public void Uninstall()
        {
            Database.ExecuteSqlRaw("IF OBJECT_ID('StorePickupPoint', 'U') IS NOT NULL DROP TABLE [StorePickupPoint]");
        }

        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            return Set<TEntity>().FromSqlRaw(commandText, parameters).ToList();
        }

        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            return Database.SqlQueryRaw<TElement>(sql, parameters).ToList();
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            if (timeout.HasValue)
                Database.SetCommandTimeout(timeout.Value);

            return Database.ExecuteSqlRaw(sql, parameters);
        }

        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            var entry = Entry(entity);
            if (entry != null)
                entry.State = EntityState.Detached;
        }

        #endregion

        #region Properties

        public virtual bool ProxyCreationEnabled
        {
            get { return false; }
            set { /* EF Core does not use proxy creation in the same way */ }
        }

        public virtual bool AutoDetectChangesEnabled
        {
            get { return ChangeTracker.AutoDetectChangesEnabled; }
            set { ChangeTracker.AutoDetectChangesEnabled = value; }
        }

        #endregion
    }
}
