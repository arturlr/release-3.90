using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Nop.Core;

namespace Nop.Data
{
    /// <summary>
    /// EF6 -> EF Core port (task 3.2).
    ///
    /// The EF6 implementation reached into the ObjectContext / MetadataWorkspace
    /// (<c>StoreItemCollection</c>, <c>EntityContainer</c>, <c>TypeUsage.Facets</c>,
    /// <c>EntityConnection</c>) to recover table names, column max lengths and decimal
    /// precision. EF Core exposes the same information through its own model
    /// (<see cref="IEntityType"/> / <see cref="IProperty"/>) plus the relational metadata
    /// extensions, so every helper below is re-based on <c>DbContext.Model</c>. Public
    /// signatures are unchanged.
    /// </summary>
    public static class DbContextExtensions
    {
        #region Utilities

        private static T InnerGetCopy<T>(IDbContext context, T currentCopy, Func<EntityEntry<T>, PropertyValues> func) where T : BaseEntity
        {
            //Get the database context
            DbContext dbContext = CastOrThrow(context);

            //Get the entity tracking object
            EntityEntry<T> entry = GetEntityOrReturnNull(currentCopy, dbContext);

            //The output 
            T output = null;

            //Try and get the values
            if (entry != null)
            {
                PropertyValues dbPropertyValues = func(entry);
                if (dbPropertyValues != null)
                {
                    output = dbPropertyValues.ToObject() as T;
                }
            }

            return output;
        }

        /// <summary>
        /// Gets the entity or return null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="currentCopy">The current copy.</param>
        /// <param name="dbContext">The db context.</param>
        /// <returns></returns>
        private static EntityEntry<T> GetEntityOrReturnNull<T>(T currentCopy, DbContext dbContext) where T : BaseEntity
        {
            return dbContext.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == currentCopy);
        }

        private static DbContext CastOrThrow(IDbContext context)
        {
            var output = context as DbContext;

            if (output == null)
            {
                throw new InvalidOperationException("Context does not support operation.");
            }

            return output;
        }

        /// <summary>
        /// Resolve an entity type in the EF Core model from either a simple CLR type name
        /// ("Product") or an assembly-qualified/full name.
        /// </summary>
        private static IEntityType FindEntityType(DbContext dbContext, string entityTypeName)
        {
            if (string.IsNullOrEmpty(entityTypeName))
                return null;

            //the EF6 implementation accepted both a plain CLR name and a fully qualified one
            var resolved = Type.GetType(entityTypeName);
            if (resolved != null)
            {
                var byClrType = dbContext.Model.FindEntityType(resolved);
                if (byClrType != null)
                    return byClrType;
            }

            return dbContext.Model.GetEntityTypes()
                .FirstOrDefault(et => et.ClrType != null &&
                                      (et.ClrType.Name == entityTypeName ||
                                       et.ClrType.FullName == entityTypeName));
        }

        /// <summary>
        /// EF Core replacement for the EF6 "field facets" lookup: returns the model properties of
        /// the named entity type whose names appear in <paramref name="columnNames"/>.
        /// </summary>
        private static IDictionary<string, IProperty> GetModelProperties(this IDbContext context,
            string entityTypeName, params string[] columnNames)
        {
            var dbContext = CastOrThrow(context);
            var entityType = FindEntityType(dbContext, entityTypeName);
            if (entityType == null)
                return new Dictionary<string, IProperty>();

            return entityType.GetProperties()
                .Where(p => columnNames != null && columnNames.Contains(p.Name))
                .GroupBy(p => p.Name)
                .ToDictionary(g => g.Key, g => g.First());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Loads the original copy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <param name="currentCopy">The current copy.</param>
        /// <returns></returns>
        public static T LoadOriginalCopy<T>(this IDbContext context, T currentCopy) where T : BaseEntity
        {
            return InnerGetCopy(context, currentCopy, e => e.OriginalValues);
        }

        /// <summary>
        /// Loads the database copy.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <param name="currentCopy">The current copy.</param>
        /// <returns></returns>
        public static T LoadDatabaseCopy<T>(this IDbContext context, T currentCopy) where T : BaseEntity
        {
            return InnerGetCopy(context, currentCopy, e => e.GetDatabaseValues());
        }

        /// <summary>
        /// Drop a plugin table
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="tableName">Table name</param>
        public static void DropPluginTable(this DbContext context, string tableName)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException("tableName");

            //drop the table
            //EF6 used Database.SqlQuery<int>. EF Core's scalar equivalent is
            //Database.SqlQueryRaw<T>, which requires the single column to be aliased "Value".
            if (context.Database
                    .SqlQueryRaw<int>("SELECT 1 AS Value FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", tableName)
                    .Any())
            {
                var dbScript = "DROP TABLE [" + tableName + "]";
                context.Database.ExecuteSqlRaw(dbScript);
            }
            context.SaveChanges();
        }

        /// <summary>
        /// Split a SQL Server script on its <c>GO</c> batch separators.
        /// </summary>
        /// <param name="script">The script to split. Null/whitespace yields nothing.</param>
        /// <returns>One string per batch, in order, with the <c>GO</c> lines removed.</returns>
        /// <remarks>
        /// <para>
        /// <b>TASK 11.2 — runtime deferral 4.10. THIS IS THE SHARED HELPER THE OTHER THREE
        /// PLUGIN CONTEXTS MUST USE (tasks 13.1, 14.4, 15.1).</b>
        /// </para>
        /// <para>
        /// <c>GO</c> is a <b>client</b> batch terminator understood by SSMS/sqlcmd, not a T-SQL
        /// statement. EF6's <c>ObjectContext.CreateDatabaseScript()</c> emitted a single
        /// unseparated batch, so every caller could send the whole thing as one command. EF
        /// Core's <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade">
        /// Database</see><c>.GenerateCreateScript()</c> — which task 3.2 substituted for it —
        /// separates statements with <c>GO</c>, so sending the script as one command fails with
        /// <c>Incorrect syntax near 'GO'</c>.
        /// </para>
        /// <para>
        /// <c>Nop.Data.Initializers.CreateTablesIfNotExist</c> already split the script; it now
        /// delegates here so there is ONE implementation. The four <b>plugin</b> contexts do
        /// <c>Database.ExecuteSqlCommand(CreateDatabaseScript())</c> in their own
        /// <c>Install()</c> and never pass through that initializer, which is what deferral 4.10
        /// records. Each of them calls <see cref="ExecuteSqlScript"/> below instead.
        /// </para>
        /// <para>
        /// Deliberately simple: only a line that is <b>exactly</b> <c>GO</c> after trimming is a
        /// separator. <c>GO 5</c> (the sqlcmd repeat count), <c>GO</c> inside a string literal or
        /// a comment, and an identifier that merely starts with <c>GO</c> are all left alone —
        /// EF Core's generator emits none of those, and a fuller T-SQL lexer would be
        /// speculation. Empty batches are dropped so a trailing <c>GO</c> cannot produce an
        /// empty command.
        /// </para>
        /// </remarks>
        public static IEnumerable<string> SplitSqlIntoBatches(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
                yield break;

            var batch = new StringBuilder();
            foreach (var line in script.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
                {
                    if (batch.ToString().Trim().Length > 0)
                        yield return batch.ToString();
                    batch.Clear();
                    continue;
                }

                batch.AppendLine(line);
            }

            if (batch.ToString().Trim().Length > 0)
                yield return batch.ToString();
        }

        /// <summary>
        /// Execute a possibly <c>GO</c>-batched SQL script, one command per batch.
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="script">The script — typically <c>IDbContext.CreateDatabaseScript()</c></param>
        /// <returns>The number of batches executed</returns>
        /// <remarks>
        /// <para>
        /// <b>TASK 11.2 — runtime deferral 4.10.</b> The one line a plugin's
        /// <c>ObjectContext.Install()</c> needs in place of EF6's
        /// <c>Database.ExecuteSqlCommand(CreateDatabaseScript())</c>. See
        /// <see cref="SplitSqlIntoBatches"/> for why the split is necessary.
        /// </para>
        /// <para>
        /// No ambient transaction is opened. That is deliberate and matches
        /// <c>CreateTablesIfNotExist</c>: a <c>GO</c> separator exists precisely because the
        /// batches must be sent independently, and some DDL cannot run inside a transaction
        /// (runtime deferral 4.11). It also matches EF6's behaviour at these call sites, which
        /// passed <c>ExecuteSqlCommand</c>'s default <c>doNotEnsureTransaction: false</c> —
        /// EF6's <c>DoNotEnsureTransaction</c> semantics.
        /// </para>
        /// </remarks>
        public static int ExecuteSqlScript(this DbContext context, string script)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var executed = 0;
            foreach (var batch in SplitSqlIntoBatches(script))
            {
                context.Database.ExecuteSqlRaw(batch);
                executed++;
            }

            return executed;
        }

        /// <summary>
        /// Get table name of entity
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">Context</param>
        /// <returns>Table name</returns>
        public static string GetTableName<T>(this IDbContext context) where T : BaseEntity
        {
            //EF6 walked the SSpace StoreItemCollection to find the store entity set and read its
            //"Table" metadata property. EF Core exposes the mapped table directly.
            var dbContext = CastOrThrow(context);

            var entityType = dbContext.Model.FindEntityType(typeof(T));
            if (entityType == null)
                throw new InvalidOperationException(
                    string.Format("Entity type '{0}' is not part of the model.", typeof(T).Name));

            return entityType.GetTableName() ?? typeof(T).Name;
        }

        /// <summary>
        /// Get column maximum length
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="entityTypeName">Entity type name</param>
        /// <param name="columnName">Column name</param>
        /// <returns>Maximum length. Null if such rule does not exist</returns>
        public static int? GetColumnMaxLength(this IDbContext context, string entityTypeName, string columnName)
        {
            var rez = GetColumnsMaxLength(context, entityTypeName, columnName);
            return rez.ContainsKey(columnName) ? rez[columnName] as int? : null;
        }

        /// <summary>
        /// Get columns maximum length
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="entityTypeName">Entity type name</param>
        /// <param name="columnNames">Column names</param>
        /// <returns></returns>
        public static IDictionary<string, int> GetColumnsMaxLength(this IDbContext context, string entityTypeName, params string[] columnNames)
        {
            //EF6: TypeUsage.Facets["MaxLength"] on CSpace String properties.
            return context.GetModelProperties(entityTypeName, columnNames)
                .Where(p => p.Value.ClrType == typeof(string) && p.Value.GetMaxLength().HasValue)
                .ToDictionary(p => p.Key, p => p.Value.GetMaxLength().Value);
        }


        /// <summary>
        /// Get maximum decimal values
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="entityTypeName">Entity type name</param>
        /// <param name="columnNames">Column names</param>
        /// <returns></returns>
        public static IDictionary<string, decimal> GetDecimalMaxValue(this IDbContext context, string entityTypeName, params string[] columnNames)
        {
            //EF6: Facets["Precision"] - Facets["Scale"] on CSpace Decimal properties.
            return context.GetModelProperties(entityTypeName, columnNames)
                .Where(p => (Nullable.GetUnderlyingType(p.Value.ClrType) ?? p.Value.ClrType) == typeof(decimal) &&
                            p.Value.GetPrecision().HasValue && p.Value.GetScale().HasValue)
                .ToDictionary(p => p.Key,
                    p => new decimal(Math.Pow(10, p.Value.GetPrecision().Value - p.Value.GetScale().Value)));
        }

        public static string DbName(this IDbContext context)
        {
            //EF6: ((IObjectContextAdapter)context).ObjectContext.Connection as EntityConnection
            //     -> connection.StoreConnection.Database
            var dbContext = context as DbContext;
            if (dbContext == null)
                return string.Empty;

            var connection = dbContext.Database.GetDbConnection();
            if (connection == null)
                return string.Empty;

            return connection.Database;
        }

        #endregion
    }
}
