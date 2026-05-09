using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nop.Core;

namespace Nop.Data
{
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
                    output = (T)dbPropertyValues.ToObject();
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
            var tableExists = context.Database.SqlQueryRaw<int>(
                "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", tableName).Any();
            if (tableExists)
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
            var dbContext = CastOrThrow(context);
            var entityType = dbContext.Model.FindEntityType(typeof(T));
            if (entityType == null)
                return typeof(T).Name;

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
            var dbContext = CastOrThrow(context);
            var result = new Dictionary<string, int>();

            var entType = Type.GetType(entityTypeName);
            if (entType == null)
                return result;

            var entityType = dbContext.Model.FindEntityType(entType);
            if (entityType == null)
                return result;

            foreach (var columnName in columnNames)
            {
                var property = entityType.FindProperty(columnName);
                if (property != null && property.GetMaxLength().HasValue)
                {
                    result[columnName] = property.GetMaxLength().Value;
                }
            }

            return result;
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
            var dbContext = CastOrThrow(context);
            var result = new Dictionary<string, decimal>();

            var entType = Type.GetType(entityTypeName);
            if (entType == null)
                return result;

            var entityType = dbContext.Model.FindEntityType(entType);
            if (entityType == null)
                return result;

            foreach (var columnName in columnNames)
            {
                var property = entityType.FindProperty(columnName);
                if (property != null)
                {
                    var precision = property.GetPrecision() ?? 18;
                    var scale = property.GetScale() ?? 0;
                    result[columnName] = new decimal(Math.Pow(10, precision - scale));
                }
            }

            return result;
        }

        public static string DbName(this IDbContext context)
        {
            var dbContext = CastOrThrow(context);
            return dbContext.Database.GetDbConnection().Database;
        }

        #endregion
    }
}
