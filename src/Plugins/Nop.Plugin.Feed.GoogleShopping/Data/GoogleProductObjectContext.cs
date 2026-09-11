using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Feed.GoogleShopping.Domain;

namespace Nop.Plugin.Feed.GoogleShopping.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>TASK 11.2 — the FIRST of the four plugin object contexts, and therefore the first place
    /// runtime deferral 4.10 actually bites.</b> The EF6 → EF Core port follows
    /// <c>Nop.Data.NopObjectContext</c> (task 3.2) member for member; the substitutions are the
    /// same ones recorded there, and the parts that are DIFFERENT are called out below. Tasks
    /// 13.1, 14.4 and 15.1 port the other three and can copy this file's shape.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>DEFERRAL 4.10 — <c>Install()</c> COULD NOT WORK, AND THE FIX IS SHARED.</b>
    /// ===================================================================================
    /// 3.90's <c>Install()</c> was
    /// <c>Database.ExecuteSqlCommand(CreateDatabaseScript())</c> — one command for the whole
    /// script, which was correct for EF6 because
    /// <c>ObjectContext.CreateDatabaseScript()</c> emitted a single unseparated batch. Task 3.2
    /// substituted EF Core's <c>Database.GenerateCreateScript()</c>, whose output is
    /// <b><c>GO</c>-batched</b> — and <c>GO</c> is a CLIENT directive, not T-SQL, so sending it as
    /// one command fails with <c>Incorrect syntax near 'GO'</c>. <c>Nop.Data</c> handled this
    /// inside <c>CreateTablesIfNotExist</c>, which a plugin's own context never passes through.
    /// </para>
    /// <para>
    /// <b>The split now lives in ONE place for all four plugin contexts:</b>
    /// <c>Nop.Data.DbContextExtensions.SplitSqlIntoBatches</c>, exposed as the
    /// <c>ExecuteSqlScript(this DbContext, string)</c> extension this <c>Install()</c> calls.
    /// <c>CreateTablesIfNotExist</c> was refactored onto the same method rather than keeping its
    /// own private copy, so the two cannot drift.
    /// </para>
    /// <para>
    /// Independently: the emitted DDL follows <b>EF Core's</b> naming and ordering conventions, so
    /// the <c>GoogleProduct</c> table this creates is not byte-identical to 3.90's. The table NAME
    /// and the key are pinned by <see cref="GoogleProductRecordMap"/>, and the columns are all
    /// plain scalars with no explicit facets, so the practical difference is column order and
    /// <c>nvarchar</c> length defaults. Recorded, not hidden.
    /// </para>
    /// <para>
    /// ===================================================================================
    /// <b>WHAT ELSE CHANGED, AND THE ONE THING THAT DID NOT</b>
    /// ===================================================================================
    /// <list type="bullet">
    /// <item><c>System.Data.Entity.DbContext</c> → <c>Microsoft.EntityFrameworkCore.DbContext</c>.
    /// The <c>(string nameOrConnectionString)</c> constructor is <b>preserved and is
    /// load-bearing</b>: <c>Nop.Web.Framework</c>'s <c>RegisterPluginDataContext</c> constructs
    /// this type with <c>Activator.CreateInstance(typeof(T), new object[]
    /// { connectionString })</c>, so removing or renaming it fails at RUNTIME with a
    /// <c>MissingMethodException</c> and no compile error anywhere. EF Core configures a context
    /// through <c>DbContextOptions</c> instead, so the string is stashed and applied in
    /// <see cref="OnConfiguring"/> — exactly as <c>NopObjectContext</c> does.</item>
    /// <item><c>OnModelCreating(DbModelBuilder)</c> → <c>OnModelCreating(ModelBuilder)</c>, and
    /// <c>modelBuilder.Configurations.Add(new GoogleProductRecordMap())</c> →
    /// <c>ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())</c>. <b>The assembly
    /// argument matters here in a way it does not in <c>NopObjectContext</c>:</b> the parameterless
    /// overload would scan the assembly that DECLARES the context, which is this one — correct
    /// today, but the explicit call states the intent and, more importantly, guarantees the scan
    /// does NOT reach <c>Nop.Data</c>'s 105 maps. Pulling those in would add every nopCommerce
    /// entity to this context's model, and <see cref="Install"/> would then generate a create
    /// script for the whole nopCommerce schema.</item>
    /// <item><c>IDbSet&lt;TEntity&gt;</c> → <c>DbSet&lt;TEntity&gt;</c> on <c>Set&lt;T&gt;</c>,
    /// matching <c>IDbContext</c> after task 3.2.</item>
    /// <item><c>Detach</c>: <c>((IObjectContextAdapter)this).ObjectContext.Detach(entity)</c> →
    /// <c>Entry(entity).State = EntityState.Detached</c>.</item>
    /// <item><c>ProxyCreationEnabled</c> / <c>AutoDetectChangesEnabled</c>:
    /// <c>this.Configuration.*</c> → <c>ChangeTracker.*</c>. EF Core has no
    /// <c>DbContextConfiguration</c>, and proxy creation is an immutable options-level decision
    /// there, so the setter maps onto the runtime analogue <c>ChangeTracker.LazyLoadingEnabled</c>
    /// and the flag is remembered so the getter round-trips — the same compromise
    /// <c>NopObjectContext</c> documents at length (runtime deferral 4.7). Neither property is
    /// read or written anywhere in this plugin; they exist because <c>IDbContext</c> declares
    /// them.</item>
    /// <item><c>ExecuteStoredProcedureList</c>, <c>SqlQuery</c> and <c>ExecuteSqlCommand</c>
    /// <b>still throw <c>NotImplementedException</c></b>, exactly as in 3.90. That is not an
    /// oversight to be tidied up: this context serves one table through
    /// <c>EfRepository&lt;GoogleProductRecord&gt;</c> and 3.90 declared the raw-SQL surface
    /// unsupported. Implementing it would be inventing behaviour the plugin never had.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Lazy-loading proxies are NOT enabled here, and that is deliberate</b> —
    /// <c>NopObjectContext</c> calls <c>UseLazyLoadingProxies()</c> because 105 entity types have
    /// <c>public virtual</c> navigations that EF6 populated automatically. <see cref="GoogleProductRecord"/>
    /// has none: ten scalar properties and no navigation at all. Turning proxies on would add a
    /// Castle DynamicProxy dependency to a plugin that has no use for one, and would make
    /// <c>Nop.Data.Extensions.GetUnproxiedEntityType</c> relevant to a type that never needs it.
    /// </para>
    /// </remarks>
    public class GoogleProductObjectContext : DbContext, IDbContext
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
        public GoogleProductObjectContext(string nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }

        /// <summary>
        /// Ctor accepting pre-built options, for hosts/tests that configure the provider
        /// themselves. Additive; mirrors the one task 3.2 added to <c>NopObjectContext</c>.
        /// </summary>
        /// <param name="options">Context options</param>
        public GoogleProductObjectContext(DbContextOptions<GoogleProductObjectContext> options)
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
            //EF6: modelBuilder.Configurations.Add(new GoogleProductRecordMap());
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
            var tableName = this.GetTableName<GoogleProductRecord>();
            //var tableName = "GoogleProduct";
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
