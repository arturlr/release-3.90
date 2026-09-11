using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Pickup.PickupInStore.Domain;

namespace Nop.Plugin.Pickup.PickupInStore.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 13.1 — the third of the four plugin object contexts (after Feed.GoogleShopping at
    /// 11.2), and one of the places runtime deferral 4.10 bites.</b> The EF6 → EF Core port is the
    /// mechanical shape task 11.2 established in
    /// <c>Nop.Plugin.Feed.GoogleShopping/Data/GoogleProductObjectContext.cs</c>; that file carries
    /// the full reasoning and is the worked example. Only what is specific to this context is noted
    /// below.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>DEFERRAL 4.10 — <c>Install()</c> COULD NOT WORK, AND THE FIX IS SHARED.</b>
    /// ===================================================================================
    /// 3.90's <c>Install()</c> was <c>Database.ExecuteSqlCommand(CreateDatabaseScript())</c> — one
    /// command for the whole script, correct for EF6 because <c>ObjectContext.CreateDatabaseScript()</c>
    /// emitted a single unseparated batch. Task 3.2 substituted EF Core's
    /// <c>Database.GenerateCreateScript()</c>, whose output is <b><c>GO</c>-batched</b>, and <c>GO</c>
    /// is a CLIENT directive, not T-SQL, so sending it as one command fails with
    /// <c>Incorrect syntax near 'GO'</c>. For a model this small EF Core also emits a <b>trailing</b>
    /// <c>GO</c>, which is on its own still fatal to a single <c>ExecuteSqlCommand</c> (11.2's measured
    /// refinement). The split lives in ONE place —
    /// <c>Nop.Data.DbContextExtensions.SplitSqlIntoBatches</c>, exposed as
    /// <c>ExecuteSqlScript(this DbContext, string)</c>, which this <c>Install()</c> calls. Do not
    /// re-derive a splitter: <c>CreateTablesIfNotExist</c> was refactored onto the same method so the
    /// two cannot drift.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>WHAT CHANGED, AND WHAT DID NOT</b>
    /// ===================================================================================
    /// <list type="bullet">
    /// <item><c>System.Data.Entity.DbContext</c> → <c>Microsoft.EntityFrameworkCore.DbContext</c>.
    /// The <c>(string nameOrConnectionString)</c> constructor is <b>preserved and load-bearing</b>:
    /// <c>Nop.Web.Framework.RegisterPluginDataContext</c> constructs this type with
    /// <c>Activator.CreateInstance(typeof(T), new object[] { connectionString })</c>, so removing or
    /// renaming it fails at RUNTIME with a <c>MissingMethodException</c> and no compile error. EF Core
    /// stashes the string and applies it in <see cref="OnConfiguring"/>.</item>
    /// <item><c>OnModelCreating(DbModelBuilder)</c> → <c>OnModelCreating(ModelBuilder)</c>, and
    /// <c>modelBuilder.Configurations.Add(new StorePickupPointMap())</c> →
    /// <c>ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())</c>. <b>The assembly
    /// argument matters:</b> it confines the scan to THIS plugin's one map, guaranteeing the model
    /// does NOT gain <c>Nop.Data</c>'s ~105 entities — which would make <see cref="Install"/> generate
    /// a create script for the whole nopCommerce schema.</item>
    /// <item><c>Set&lt;T&gt;</c> returns <c>DbSet&lt;TEntity&gt;</c> (was EF6 <c>IDbSet&lt;TEntity&gt;</c>),
    /// matching <c>IDbContext</c> after task 3.2.</item>
    /// <item><c>Detach</c>: <c>((IObjectContextAdapter)this).ObjectContext.Detach(entity)</c> →
    /// <c>Entry(entity).State = EntityState.Detached</c>.</item>
    /// <item><c>ProxyCreationEnabled</c> / <c>AutoDetectChangesEnabled</c>: <c>this.Configuration.*</c>
    /// → <c>ChangeTracker.*</c>, the same compromise <c>NopObjectContext</c> and
    /// <c>GoogleProductObjectContext</c> document (deferral 4.7). Neither property is read or written
    /// anywhere in this plugin; they exist because <c>IDbContext</c> declares them.</item>
    /// <item><c>ExecuteStoredProcedureList</c>, <c>SqlQuery</c> and <c>ExecuteSqlCommand</c>
    /// <b>still throw <c>NotImplementedException</c></b>, exactly as in 3.90 — this context serves one
    /// table through <c>EfRepository&lt;StorePickupPoint&gt;</c> and 3.90 declared the raw-SQL surface
    /// unsupported. Implementing it would be inventing behaviour the plugin never had.</item>
    /// </list>
    /// </para>
    /// <para>
    /// Lazy-loading proxies are NOT enabled: <see cref="StorePickupPoint"/> has only scalar properties
    /// and no navigation, so a proxy dependency would serve nothing — the same decision as
    /// <c>GoogleProductObjectContext</c>.
    /// </para>
    /// </remarks>
    public class StorePickupPointObjectContext : DbContext, IDbContext
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
        public StorePickupPointObjectContext(string nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }

        /// <summary>
        /// Ctor accepting pre-built options, for hosts/tests that configure the provider
        /// themselves. Additive; mirrors the one task 3.2 added to <c>NopObjectContext</c>.
        /// </summary>
        /// <param name="options">Context options</param>
        public StorePickupPointObjectContext(DbContextOptions<StorePickupPointObjectContext> options)
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

        /// <summary>
        /// Add entity to the configuration of the model for a derived context before it is locked down
        /// </summary>
        /// <param name="modelBuilder">The builder that defines the model for the context being created</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //EF6: modelBuilder.Configurations.Add(new StorePickupPointMap());
            //The assembly argument confines the scan to THIS plugin - see the class remarks.
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Generates a data definition language script that creates schema objects
        /// </summary>
        /// <returns>A DDL script</returns>
        public string CreateDatabaseScript()
        {
            //EF6: ((IObjectContextAdapter)this).ObjectContext.CreateDatabaseScript()
            //NOTE the result is GO-batched - see deferral 4.10 in the class remarks. Callers must
            //go through DbContextExtensions.ExecuteSqlScript, as Install() below does.
            return Database.GenerateCreateScript();
        }

        /// <summary>
        /// Returns a DbSet instance for access to entities of the given type in the context and the underlying store
        /// </summary>
        /// <typeparam name="TEntity">The type entity for which a set should be returned</typeparam>
        /// <returns>A set for the given entity type</returns>
        public new DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }

        /// <summary>
        /// Install object context
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
        /// Uninstall object context
        /// </summary>
        public void Uninstall()
        {
            //drop the table
            this.DropPluginTable(this.GetTableName<StorePickupPoint>());
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
