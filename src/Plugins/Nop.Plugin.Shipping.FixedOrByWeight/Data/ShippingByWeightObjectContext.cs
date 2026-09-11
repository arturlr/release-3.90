using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Shipping.FixedOrByWeight.Domain;

namespace Nop.Plugin.Shipping.FixedOrByWeight.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 14.4 — the 14.4 quarter of runtime deferral 4.10, the third of the four plugin
    /// object contexts.</b> The EF6 → EF Core port follows <c>Nop.Data.NopObjectContext</c>
    /// (task 3.2) and the first plugin port
    /// <c>Nop.Plugin.Feed.GoogleShopping.Data.GoogleProductObjectContext</c> (task 11.2, §92.3)
    /// member for member. The substitutions are the same recorded there; only what is DIFFERENT
    /// about this context is called out.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>DEFERRAL 4.10 — <c>Install()</c> COULD NOT WORK, AND THE FIX IS SHARED.</b>
    /// ===================================================================================
    /// 3.90's <c>Install()</c> was <c>Database.ExecuteSqlCommand(CreateDatabaseScript())</c> — one
    /// command for the whole script, correct for EF6 because
    /// <c>ObjectContext.CreateDatabaseScript()</c> emitted a single unseparated batch. Task 3.2
    /// substituted EF Core's <c>Database.GenerateCreateScript()</c>, whose output is
    /// <c>GO</c>-batched — and <c>GO</c> is a CLIENT directive, not T-SQL, so sending it as one
    /// command fails with <c>Incorrect syntax near 'GO'</c>. The split lives in ONE place for all
    /// four plugin contexts: <c>Nop.Data.DbContextExtensions.SplitSqlIntoBatches</c>, exposed as
    /// the <c>ExecuteSqlScript(this DbContext, string)</c> extension this <c>Install()</c> calls.
    /// The splitter is NOT re-derived here.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>WHAT ELSE CHANGED, AND THE ONE THING THAT DID NOT</b>
    /// ===================================================================================
    /// <list type="bullet">
    /// <item><c>System.Data.Entity.DbContext</c> → <c>Microsoft.EntityFrameworkCore.DbContext</c>.
    /// The <c>(string nameOrConnectionString)</c> constructor is <b>preserved and load-bearing</b>:
    /// <c>Nop.Web.Framework</c>'s <c>RegisterPluginDataContext</c> (called by
    /// <c>DependencyRegistrar</c> with named parameter
    /// <c>"nop_object_context_shipping_weight_zip"</c>) constructs this type with
    /// <c>Activator.CreateInstance(typeof(T), new object[] { connectionString })</c>. EF Core
    /// configures a context through <c>DbContextOptions</c>, so the string is stashed and applied
    /// in <see cref="OnConfiguring"/> — exactly as <c>GoogleProductObjectContext</c> does.</item>
    /// <item><c>OnModelCreating(DbModelBuilder)</c> → <c>OnModelCreating(ModelBuilder)</c>, and
    /// <c>modelBuilder.Configurations.Add(new ShippingByWeightRecordMap())</c> →
    /// <c>ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())</c>. The assembly
    /// argument confines the scan to THIS plugin (one entity type,
    /// <see cref="ShippingByWeightRecord"/>) so <see cref="Install"/> generates a create script
    /// for the plugin's ONE table, not the whole nopCommerce schema.</item>
    /// <item><c>IDbSet&lt;T&gt;</c> → <c>DbSet&lt;T&gt;</c>; <c>ObjectContext.Detach</c> →
    /// <c>Entry(entity).State = EntityState.Detached</c>;
    /// <c>this.Configuration.ProxyCreationEnabled</c>/<c>AutoDetectChangesEnabled</c> →
    /// <c>ChangeTracker.*</c> with the same compromise <c>NopObjectContext</c> documents
    /// (deferral 4.7). Neither property is read or written anywhere in this plugin; they exist
    /// because <c>IDbContext</c> declares them.</item>
    /// <item><c>ExecuteStoredProcedureList</c>, <c>SqlQuery</c> and <c>ExecuteSqlCommand</c> still
    /// throw <c>NotImplementedException</c>, exactly as in 3.90 — this context serves one table
    /// through <c>EfRepository&lt;ShippingByWeightRecord&gt;</c> and 3.90 declared the raw-SQL
    /// surface unsupported.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Lazy-loading proxies are NOT enabled here</b>, as in <c>GoogleProductObjectContext</c>:
    /// <see cref="ShippingByWeightRecord"/> is all scalar with no navigation.
    /// </para>
    /// </remarks>
    public class ShippingByWeightObjectContext : DbContext, IDbContext
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
        public ShippingByWeightObjectContext(string nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }

        /// <summary>
        /// Ctor accepting pre-built options, for hosts/tests that configure the provider
        /// themselves. Additive; mirrors the one task 3.2 added to <c>NopObjectContext</c>.
        /// </summary>
        /// <param name="options">Context options</param>
        public ShippingByWeightObjectContext(DbContextOptions<ShippingByWeightObjectContext> options)
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
            //EF6: modelBuilder.Configurations.Add(new ShippingByWeightRecordMap());
            //The assembly argument confines the scan to THIS plugin - see the class remarks.
            modelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());

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
            var tableName = this.GetTableName<ShippingByWeightRecord>();
            //var tableName = "ShippingByWeight";
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
