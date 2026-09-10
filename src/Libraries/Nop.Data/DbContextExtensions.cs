using System;
using System.Collections.Generic;
using System.Linq;
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
