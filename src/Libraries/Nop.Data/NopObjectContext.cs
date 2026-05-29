using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nop.Core;
using Nop.Data.Mapping;

namespace Nop.Data
{
    /// <summary>
    /// Represents the Nop database context (EF Core)
    /// </summary>
    public class NopObjectContext : DbContext, IDbContext
    {
        #region Ctor

        public NopObjectContext(DbContextOptions<NopObjectContext> options)
            : base(options)
        {
        }

        #endregion

        #region Utilities

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Dynamically load all IEntityTypeConfiguration<T> implementations from this assembly
            var typesToRegister = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => !string.IsNullOrEmpty(type.Namespace))
                .Where(type => type.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

            foreach (var type in typesToRegister)
            {
                dynamic configurationInstance = Activator.CreateInstance(type);
                modelBuilder.ApplyConfiguration(configurationInstance);
            }

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
            var alreadyAttached = Set<TEntity>().Local.FirstOrDefault(x => x.Id == entity.Id);
            if (alreadyAttached == null)
            {
                Set<TEntity>().Attach(entity);
                return entity;
            }

            return alreadyAttached;
        }

        #endregion

        #region Methods

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
        /// Execute stored procedure and load a list of entities at the end
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="commandText">Command text</param>
        /// <param name="parameters">Parameters</param>
        /// <returns>Entities</returns>
        public IList<TEntity> ExecuteStoredProcedureList<TEntity>(string commandText, params object[] parameters) where TEntity : BaseEntity, new()
        {
            // Build command text with parameters
            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i <= parameters.Length - 1; i++)
                {
                    var p = parameters[i] as DbParameter;
                    if (p == null)
                        throw new Exception("Not supported parameter type");

                    commandText += i == 0 ? " " : ", ";
                    commandText += "@" + p.ParameterName;
                    if (p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output)
                    {
                        commandText += " output";
                    }
                }
            }

            var result = Set<TEntity>().FromSqlRaw(commandText, parameters).ToList();

            // Performance: disable auto detect changes while attaching
            bool acd = AutoDetectChangesEnabled;
            try
            {
                AutoDetectChangesEnabled = false;
                for (int i = 0; i < result.Count; i++)
                    result[i] = AttachEntityToContext(result[i]);
            }
            finally
            {
                AutoDetectChangesEnabled = acd;
            }

            return result;
        }

        /// <summary>
        /// Creates a raw SQL query that will return elements of the given generic type.
        /// </summary>
        /// <typeparam name="TElement">The type of object returned by the query.</typeparam>
        /// <param name="sql">The SQL query string.</param>
        /// <param name="parameters">The parameters to apply to the SQL query string.</param>
        /// <returns>Result</returns>
        public IList<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            // In EF Core, raw SQL for non-entity types uses Database.SqlQueryRaw (EF Core 8+)
            return Database.SqlQueryRaw<TElement>(sql, parameters).ToList();
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
                previousTimeout = Database.GetCommandTimeout();
                Database.SetCommandTimeout(timeout);
            }

            int result;
            if (doNotEnsureTransaction)
            {
                result = Database.ExecuteSqlRaw(sql, parameters);
            }
            else
            {
                // EF Core ExecuteSqlRaw always wraps in a transaction if one isn't already present
                result = Database.ExecuteSqlRaw(sql, parameters);
            }

            if (timeout.HasValue)
            {
                Database.SetCommandTimeout(previousTimeout);
            }

            return result;
        }

        /// <summary>
        /// Detach an entity from the context (stop tracking)
        /// </summary>
        /// <param name="entity">Entity</param>
        public void Detach(object entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var entry = Entry(entity);
            if (entry != null)
                entry.State = EntityState.Detached;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether auto detect changes setting is enabled
        /// </summary>
        public virtual bool AutoDetectChangesEnabled
        {
            get { return ChangeTracker.AutoDetectChangesEnabled; }
            set { ChangeTracker.AutoDetectChangesEnabled = value; }
        }

        #endregion
    }
}
