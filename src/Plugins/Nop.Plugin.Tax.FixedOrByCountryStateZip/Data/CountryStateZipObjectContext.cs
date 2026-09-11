using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Tax.FixedOrByCountryStateZip.Domain;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 15.1 — the LAST of the four plugin object contexts deferral 4.10 names.</b> The
    /// EF6 → EF Core port is the same one task 11.2 did for
    /// <c>Nop.Plugin.Feed.GoogleShopping/Data/GoogleProductObjectContext.cs</c>, member for
    /// member; that file is the worked example this one follows. The substitutions all originate
    /// with <c>Nop.Data.NopObjectContext</c> (task 3.2).
    /// </para>
    /// <para>
    /// <b>DEFERRAL 4.10 — <c>Install()</c> could not work, and the fix is shared.</b> 3.90's
    /// <c>Install()</c> did <c>Database.ExecuteSqlCommand(CreateDatabaseScript())</c> in one shot,
    /// which was correct for EF6 because <c>ObjectContext.CreateDatabaseScript()</c> emitted a
    /// single unseparated batch. Task 3.2 substituted EF Core's
    /// <c>Database.GenerateCreateScript()</c>, whose output is <b><c>GO</c>-batched</b> — and
    /// <c>GO</c> is a CLIENT directive, not T-SQL, so sending it as one command fails with
    /// <c>Incorrect syntax near 'GO'</c>. The split lives in ONE place for all four plugin
    /// contexts: <c>Nop.Data.DbContextExtensions.SplitSqlIntoBatches</c>, exposed as the
    /// <c>ExecuteSqlScript(this DbContext, string)</c> extension <see cref="Install"/> calls.
    /// </para>
    /// <para>
    /// Independently: the emitted DDL follows <b>EF Core's</b> naming/ordering conventions, so the
    /// <c>TaxRate</c> table this creates is not byte-identical to 3.90's. The table NAME, key and
    /// the <c>Percentage</c> precision are pinned by <see cref="TaxRateMap"/>; the practical
    /// difference is column order and <c>nvarchar</c> length defaults. Recorded, not hidden.
    /// </para>
    /// <para>
    /// <b>What else changed, and the one thing that did not:</b>
    /// <list type="bullet">
    /// <item><c>System.Data.Entity.DbContext</c> → <c>Microsoft.EntityFrameworkCore.DbContext</c>.
    /// The <c>(string nameOrConnectionString)</c> constructor is <b>preserved and load-bearing</b>:
    /// <c>Nop.Web.Framework</c>'s <c>RegisterPluginDataContext</c> constructs this type with
    /// <c>Activator.CreateInstance(typeof(T), new object[] { connectionString })</c>, so removing
    /// or renaming it fails at RUNTIME with a <c>MissingMethodException</c> and no compile error.
    /// EF Core configures a context through <c>DbContextOptions</c>, so the string is stashed and
    /// applied in <see cref="OnConfiguring"/>.</item>
    /// <item><c>OnModelCreating(DbModelBuilder)</c> → <c>OnModelCreating(ModelBuilder)</c>, and
    /// <c>modelBuilder.Configurations.Add(new TaxRateMap())</c> →
    /// <c>ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())</c>. The assembly
    /// argument confines the scan to THIS plugin, so it cannot reach <c>Nop.Data</c>'s 105 maps
    /// (which would add every nopCommerce entity to this context's model and make
    /// <see cref="Install"/> generate a create script for the whole schema).</item>
    /// <item><c>IDbSet&lt;TEntity&gt;</c> → <c>DbSet&lt;TEntity&gt;</c> on <c>Set&lt;T&gt;</c>.</item>
    /// <item><c>Detach</c>: <c>((IObjectContextAdapter)this).ObjectContext.Detach(entity)</c> →
    /// <c>Entry(entity).State = EntityState.Detached</c>.</item>
    /// <item><c>ProxyCreationEnabled</c> / <c>AutoDetectChangesEnabled</c>:
    /// <c>this.Configuration.*</c> → a remembered field + <c>ChangeTracker.*</c>, as
    /// <c>GoogleProductObjectContext</c> documents (deferral 4.7). Neither is read or written in
    /// this plugin; they exist because <c>IDbContext</c> declares them.</item>
    /// <item><c>ExecuteStoredProcedureList</c>, <c>SqlQuery</c> and <c>ExecuteSqlCommand</c>
    /// <b>still throw <c>NotImplementedException</c></b>, exactly as in 3.90: this context serves
    /// one table through <c>EfRepository&lt;TaxRate&gt;</c> and 3.90 declared the raw-SQL surface
    /// unsupported. Implementing it would be inventing behaviour the plugin never had.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Lazy-loading proxies are NOT enabled here, deliberately: <see cref="TaxRate"/> is six scalar
    /// properties with no navigation at all, so there is nothing to lazy-load.
    /// </para>
    /// </remarks>
    public class CountryStateZipObjectContext : DbContext, IDbContext
    {
        #region Fields

        private readonly string _nameOrConnectionString;

        //EF Core has no DbContextConfiguration.ProxyCreationEnabled; see the property below.
        private bool _proxyCreationEnabled = true;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor. DO NOT REMOVE OR RESHAPE - Nop.Web.Framework's RegisterPluginDataContext
        /// constructs this type reflectively with exactly this signature.
        /// </summary>
        public CountryStateZipObjectContext(string nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }

        /// <summary>
        /// Ctor accepting pre-built options, for hosts/tests that configure the provider
        /// themselves. Additive; mirrors the one task 3.2 added to <c>NopObjectContext</c>.
        /// </summary>
        /// <param name="options">Context options</param>
        public CountryStateZipObjectContext(DbContextOptions<CountryStateZipObjectContext> options)
            : base(options)
        {
        }

        #endregion

        #region Utilities

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //when the context was built from a DbContextOptions instance the provider is already
            //configured and _nameOrConnectionString is null - do not override it.
            if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_nameOrConnectionString))
                optionsBuilder.UseSqlServer(_nameOrConnectionString);

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //EF6: modelBuilder.Configurations.Add(new TaxRateMap());
            //The assembly argument confines the scan to THIS plugin - see the class remarks.
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create database script
        /// </summary>
        /// <returns>SQL to generate the plugin's table</returns>
        public string CreateDatabaseScript()
        {
            //EF6: ((IObjectContextAdapter)this).ObjectContext.CreateDatabaseScript()
            //NOTE the result is GO-batched - see deferral 4.10 in the class remarks. Callers must
            //go through DbContextExtensions.ExecuteSqlScript, as Install() below does.
            return Database.GenerateCreateScript();
        }

        /// <summary>
        /// Get DbSet
        /// </summary>
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
            //RUNTIME DEFERRAL 4.10: one command PER GO BATCH. 3.90's single
            //Database.ExecuteSqlCommand(dbScript) throws "Incorrect syntax near 'GO'" against the
            //EF Core script. The split lives in Nop.Data so all four plugin contexts share it.
            this.ExecuteSqlScript(CreateDatabaseScript());
            SaveChanges();
        }

        /// <summary>
        /// Uninstall
        /// </summary>
        public void Uninstall()
        {
            //drop the table
            var tableName = this.GetTableName<TaxRate>();
            //var tableName = "TaxRate";
            this.DropPluginTable(tableName);
        }

        /// <summary>
        /// Execute stores procedure and load a list of entities at the end
        /// </summary>
        /// <remarks>Unsupported by this context, as in 3.90.</remarks>
        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a raw SQL query that will return elements of the given generic type.
        /// </summary>
        /// <remarks>Unsupported by this context, as in 3.90.</remarks>
        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the given DDL/DML command against the database.
        /// </summary>
        /// <remarks>Unsupported by this context, as in 3.90.</remarks>
        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Detach an entity
        /// </summary>
        /// <param name="entity">Entity</param>
        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException("entity");

            //EF6: ((IObjectContextAdapter)this).ObjectContext.Detach(entity)
            Entry(entity).State = EntityState.Detached;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether proxy creation setting is enabled (used in EF)
        /// </summary>
        /// <remarks>See the class remarks: EF Core cannot toggle proxy creation per instance, so
        /// this maps onto the runtime analogue and the flag is remembered.</remarks>
        public virtual bool ProxyCreationEnabled
        {
            get
            {
                return _proxyCreationEnabled;
            }
            set
            {
                _proxyCreationEnabled = value;
                ChangeTracker.LazyLoadingEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether auto detect changes setting is enabled (used in EF)
        /// </summary>
        public virtual bool AutoDetectChangesEnabled
        {
            get
            {
                return ChangeTracker.AutoDetectChangesEnabled;
            }
            set
            {
                ChangeTracker.AutoDetectChangesEnabled = value;
            }
        }

        #endregion
    }
}
