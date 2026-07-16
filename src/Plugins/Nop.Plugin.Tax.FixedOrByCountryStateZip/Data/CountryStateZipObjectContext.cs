using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    public class CountryStateZipObjectContext : DbContext, IDbContext
    {
        #region Ctor

        public CountryStateZipObjectContext(DbContextOptions<CountryStateZipObjectContext> options)
            : base(options)
        {
        }

        public CountryStateZipObjectContext(string connectionString)
            : base(GetOptions(connectionString))
        {
        }

        private static DbContextOptions<CountryStateZipObjectContext> GetOptions(string connectionString)
        {
            return new DbContextOptionsBuilder<CountryStateZipObjectContext>()
                .UseSqlServer(connectionString)
                .Options;
        }

        #endregion

        #region Utilities

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TaxRateMap());
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
            //create the table
            var dbScript = CreateDatabaseScript();
            Database.ExecuteSqlRaw(dbScript);
            SaveChanges();
        }

        /// <summary>
        /// Uninstall
        /// </summary>
        public void Uninstall()
        {
            //drop the table
            Database.ExecuteSqlRaw("IF OBJECT_ID('TaxRate', 'U') IS NOT NULL DROP TABLE [TaxRate]");
        }

        /// <summary>
        /// Execute stores procedure and load a list of entities at the end
        /// </summary>
        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            return Set<TEntity>().FromSqlRaw(commandText, parameters).ToList();
        }

        /// <summary>
        /// Creates a raw SQL query that will return elements of the given generic type.
        /// </summary>
        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            return Database.SqlQueryRaw<TElement>(sql, parameters).ToList();
        }

        /// <summary>
        /// Executes the given DDL/DML command against the database.
        /// </summary>
        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            if (timeout.HasValue)
                Database.SetCommandTimeout(timeout.Value);

            return Database.ExecuteSqlRaw(sql, parameters);
        }

        /// <summary>
        /// Detach an entity
        /// </summary>
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
