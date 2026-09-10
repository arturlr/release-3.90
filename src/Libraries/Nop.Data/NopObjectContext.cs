using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Nop.Core;

namespace Nop.Data
{
    /// <summary>
    /// Object context
    /// </summary>
    /// <remarks>
    /// EF6 -> EF Core port (task 3.2). Base type changed from
    /// <c>System.Data.Entity.DbContext</c> to <see cref="Microsoft.EntityFrameworkCore.DbContext"/>.
    /// The <c>NopObjectContext(string nameOrConnectionString)</c> constructor is preserved -
    /// EF Core configures a context through <c>DbContextOptions</c> rather than a connection
    /// string, so the connection string is stashed and applied in <see cref="OnConfiguring"/>.
    /// </remarks>
    public class NopObjectContext : DbContext, IDbContext
    {
        #region Fields

        private readonly string _nameOrConnectionString;

        //EF Core has no DbContextConfiguration.ProxyCreationEnabled; see the property below.
        private bool _proxyCreationEnabled = true;

        #endregion

        #region Ctor

        public NopObjectContext(string nameOrConnectionString)
        {
            _nameOrConnectionString = nameOrConnectionString;
        }

        /// <summary>
        /// Ctor accepting pre-built options, for hosts/tests that configure the provider
        /// themselves (e.g. a different provider or an in-memory store).
        /// </summary>
        /// <param name="options">Context options</param>
        public NopObjectContext(DbContextOptions<NopObjectContext> options)
            : base(options)
        {
        }

        #endregion

        #region Utilities

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //LAZY LOADING (runtime deferral 4.7). EF6 created dynamic proxies by default, so
            //every `public virtual` navigation on Nop.Core.Domain.** loaded on first access.
            //EF Core has no proxy layer in the core package; Microsoft.EntityFrameworkCore.Proxies
            //plus this call restores that behavior.
            //
            //DELIBERATELY OUTSIDE the IsConfigured guard below. DbContextOptionsBuilder.IsConfigured
            //reports whether a DATABASE PROVIDER has been selected - it is true for a context built
            //from a DbContextOptions<NopObjectContext> instance (tests, alternate hosts). Putting
            //this call inside the guard would therefore give proxies to the connection-string
            //constructor ONLY and leave options-built contexts with the silent null-navigation
            //behavior this fix exists to remove. Lazy loading is orthogonal to provider selection,
            //so it is applied on every construction path. The call is idempotent - a caller that
            //already enabled proxies on the options is unaffected.
            optionsBuilder.UseLazyLoadingProxies();

            //when the context was built from a DbContextOptions instance the provider is
            //already configured and _nameOrConnectionString is null - do not override it.
            if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_nameOrConnectionString))
                optionsBuilder.UseSqlServer(_nameOrConnectionString);

            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //EF6 reflected over the assembly looking for NopEntityTypeConfiguration<> subclasses
            //and fed each one to modelBuilder.Configurations.Add(...). EF Core has a built-in
            //equivalent: every IEntityTypeConfiguration<T> in the assembly is discovered and
            //applied. NopEntityTypeConfiguration<T> implements IEntityTypeConfiguration<T>, so
            //all Mapping/**/*Map.cs classes are picked up by this single call.
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Attach an entity to the context or return an already attached entity (if it was already attached)
        /// </summary>
        /// <typeparam name="TEntity">TEntity</typeparam>
        /// <param name="entity">Entity</param>
        /// <returns>Attached entity</returns>
        protected virtual TEntity AttachEntityToContext<TEntity>(TEntity entity) where TEntity : BaseEntity, new()
        {
            //little hack here until Entity Framework really supports stored procedures
            //otherwise, navigation properties of loaded entities are not loaded until an entity is attached to the context
            var alreadyAttached = Set<TEntity>().Local.FirstOrDefault(x => x.Id == entity.Id);
            if (alreadyAttached == null)
            {
                //attach new entity
                Set<TEntity>().Attach(entity);
                return entity;
            }

            //entity is already loaded
            return alreadyAttached;
        }

        /// <summary>
        /// Determines whether a type is read from a single result-set column rather than
        /// mapped property-by-property.
        /// </summary>
        private static bool IsSimpleType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return underlying.IsPrimitive
                   || underlying.IsEnum
                   || underlying == typeof(string)
                   || underlying == typeof(decimal)
                   || underlying == typeof(DateTime)
                   || underlying == typeof(DateTimeOffset)
                   || underlying == typeof(TimeSpan)
                   || underlying == typeof(Guid)
                   || underlying == typeof(byte[]);
        }

        /// <summary>
        /// Binds the supplied parameters onto the command and rewrites EF6-style positional
        /// placeholders ({0}, {1}, ...) into named parameters.
        /// </summary>
        private static string PrepareCommand(string sql, object[] parameters, DbCommand command)
        {
            if (parameters == null || parameters.Length == 0)
                return sql;

            for (var i = 0; i < parameters.Length; i++)
            {
                var existing = parameters[i] as DbParameter;
                if (existing != null)
                {
                    //already a provider parameter (this is how the stored-procedure call sites
                    //pass values) - the SQL text already references it by name
                    command.Parameters.Add(existing);
                    continue;
                }

                var name = "@p" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = parameters[i] ?? DBNull.Value;
                command.Parameters.Add(parameter);

                sql = sql.Replace("{" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}", name);
            }

            return sql;
        }

        /// <summary>
        /// Runs a raw SQL query and materializes the rows.
        /// </summary>
        /// <remarks>
        /// EF6 offered <c>Database.SqlQuery&lt;T&gt;</c> for ANY type - scalars, and arbitrary
        /// non-entity classes materialized by column-name/property-name matching. EF Core has no
        /// equivalent: <c>Database.SqlQueryRaw&lt;T&gt;</c> is restricted to scalar types (and
        /// requires the column be aliased <c>Value</c>), and <c>FromSqlRaw</c> only works for
        /// types present in the model. To keep <c>IDbContext.SqlQuery&lt;TElement&gt;</c>'s
        /// signature and behavior intact for its non-entity call sites
        /// (<c>PictureService.HashItem</c>, <c>ProductTagService.ProductTagWithCount</c>) this
        /// drops to ADO.NET over the context's own connection and reuses the same
        /// reflection-based row mapper as <see cref="DataReaderExtensions"/>.
        /// </remarks>
        protected virtual IEnumerable<TElement> ExecuteRawSqlQuery<TElement>(string sql, object[] parameters)
        {
            var results = new List<TElement>();
            var elementType = typeof(TElement);
            var simple = IsSimpleType(elementType);
            var targetType = Nullable.GetUnderlyingType(elementType) ?? elementType;

            var connection = Database.GetDbConnection();
            var closeWhenDone = connection.State != ConnectionState.Open;
            if (closeWhenDone)
                connection.Open();

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = PrepareCommand(sql, parameters, command);

                    var commandTimeout = Database.GetCommandTimeout();
                    if (commandTimeout.HasValue)
                        command.CommandTimeout = commandTimeout.Value;

                    var currentTransaction = Database.CurrentTransaction;
                    if (currentTransaction != null)
                        command.Transaction = currentTransaction.GetDbTransaction();

                    using (var reader = command.ExecuteReader())
                    {
                        if (simple)
                        {
                            while (reader.Read())
                            {
                                if (reader.IsDBNull(0))
                                {
                                    results.Add(default(TElement));
                                    continue;
                                }

                                var value = reader.GetValue(0);
                                if (targetType.IsEnum)
                                    value = Enum.ToObject(targetType, value);
                                else if (!targetType.IsInstanceOfType(value))
                                    value = Convert.ChangeType(value, targetType,
                                        System.Globalization.CultureInfo.InvariantCulture);

                                results.Add((TElement)value);
                            }
                        }
                        else
                        {
                            var propertyLookup = elementType
                                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .GroupBy(p => p.Name.ToLowerInvariant())
                                .ToDictionary(g => g.Key, g => g.First());

                            while (reader.Read())
                            {
                                var instance = Activator.CreateInstance<TElement>();
                                reader.DataReaderToObject(instance, null, propertyLookup);
                                results.Add(instance);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (closeWhenDone)
                    connection.Close();
            }

            return results;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Create database script
        /// </summary>
        /// <returns>SQL to generate database</returns>
        public string CreateDatabaseScript()
        {
            //EF6: ((IObjectContextAdapter)this).ObjectContext.CreateDatabaseScript()
            return Database.GenerateCreateScript();
        }

        /// <summary>
        /// Get DbSet
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <returns>DbSet</returns>
        public new DbSet<TEntity> Set<TEntity>() where TEntity : BaseEntity
        {
            return base.Set<TEntity>();
        }
        
        /// <summary>
        /// Execute stores procedure and load a list of entities at the end
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="commandText">Command text</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Entities</returns>
        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            //add parameters to command
            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    var p = parameters[i] as DbParameter;
                    if (p == null)
                        throw new Exception("Not support parameter type");

                    commandText += i == 0 ? " " : ", ";

                    commandText += "@" + p.ParameterName;
                    if (p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output)
                    {
                        //output parameter
                        commandText += " output";
                    }
                }
            }

            //EF6: Database.SqlQuery<TEntity>(commandText, parameters) - untracked entity
            //materialization. EF Core equivalent is DbSet<TEntity>.FromSqlRaw; AsNoTracking
            //preserves the EF6 behavior of not tracking the stored-procedure results, which
            //matters because the loop below re-attaches them deliberately.
            var result = Set<TEntity>().FromSqlRaw(commandText, parameters ?? new object[0])
                .AsNoTracking()
                .ToList();

            //performance hack applied as described here - http://www.nopcommerce.com/boards/t/25483/fix-very-important-speed-improvement.aspx
            bool acd = this.ChangeTracker.AutoDetectChangesEnabled;
            try
            {
                this.ChangeTracker.AutoDetectChangesEnabled = false;

                for (int i = 0; i < result.Count; i++)
                    result[i] = AttachEntityToContext(result[i]);
            }
            finally
            {
                this.ChangeTracker.AutoDetectChangesEnabled = acd;
            }

            return result;
        }

        /// <summary>
        /// Creates a raw SQL query that will return elements of the given generic type.  The type can be any type that has properties that match the names of the columns returned from the query, or can be a simple primitive type. The type does not have to be an entity type. The results of this query are never tracked by the context even if the type of object returned is an entity type.
        /// </summary>
        /// <typeparam name="TElement">The type of object returned by the query.</typeparam>
        /// <param name="sql">The SQL query string.</param>
        /// <param name="parameters">The parameters to apply to the SQL query string.</param>
        /// <returns>Result</returns>
        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            return ExecuteRawSqlQuery<TElement>(sql, parameters);
        }
    
        /// <summary>
        /// Executes the given DDL/DML command against the database.
        /// </summary>
        /// <param name="sql">The command string</param>
        /// <param name="doNotEnsureTransaction">false - the transaction creation is not ensured; true - the transaction creation is ensured.</param>
        /// <param name="timeout">Timeout value, in seconds. A null value indicates that the default value of the underlying provider will be used</param>
        /// <param name="parameters">The parameters to apply to the command string.</param>
        /// <returns>The result returned by the database after executing the command.</returns>
        public int ExecuteSqlCommand(string sql, bool doNotEnsureTransaction = false, int? timeout = null, params object[] parameters)
        {
            int? previousTimeout = null;
            if (timeout.HasValue)
            {
                //store previous timeout
                previousTimeout = Database.GetCommandTimeout();
                Database.SetCommandTimeout(timeout);
            }

            var parameterValues = parameters ?? new object[0];

            int result;
            if (doNotEnsureTransaction || Database.CurrentTransaction != null)
            {
                //EF Core's ExecuteSqlRaw does not open its own transaction, which is exactly
                //EF6's TransactionalBehavior.DoNotEnsureTransaction.
                result = Database.ExecuteSqlRaw(sql, parameterValues);
            }
            else
            {
                //EF6's TransactionalBehavior.EnsureTransaction had no EF Core counterpart;
                //an explicit transaction reproduces it.
                using (var transaction = Database.BeginTransaction())
                {
                    result = Database.ExecuteSqlRaw(sql, parameterValues);
                    transaction.Commit();
                }
            }

            if (timeout.HasValue)
            {
                //Set previous timeout back
                Database.SetCommandTimeout(previousTimeout);
            }

            //return result
            return result;
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
        /// <remarks>
        /// EF6 mapped this straight onto <c>DbContextConfiguration.ProxyCreationEnabled</c>,
        /// where <c>false</c> suppressed proxy creation for subsequently materialized entities
        /// and therefore also suppressed their lazy loading.
        /// <para>
        /// Lazy-loading proxies ARE now enabled - see <see cref="OnConfiguring"/> and runtime
        /// deferral 4.7. In EF Core proxy creation itself is an immutable options-level decision
        /// and cannot be toggled per-instance at runtime, so the setter maps onto the runtime
        /// analogue <c>ChangeTracker.LazyLoadingEnabled</c> and the flag is remembered so the
        /// getter round-trips. Entities materialized while this is <c>false</c> are still proxy
        /// instances, but their navigations will not self-load.
        /// </para>
        /// <para>
        /// This now reproduces the observable intent of the EF6 call site:
        /// <c>PictureService.GetPictureHashes</c> sets it to <c>false</c> as a deliberate
        /// performance hack so a bulk picture scan does not trigger per-row navigation loads.
        /// Under EF6 that avoided proxy creation; here it avoids the lazy loads. The residual
        /// difference is allocation-level only (a proxy object is still created), not
        /// query-level - no extra SELECT is issued either way.
        /// </para>
        /// </remarks>
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
