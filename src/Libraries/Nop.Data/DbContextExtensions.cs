using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nop.Core;

namespace Nop.Data
{
    public static class DbContextExtensions
    {
        #region Utilities

        private static T InnerGetCopy<T>(IDbContext context, T currentCopy, Func<EntityEntry<T>, T> func) where T : BaseEntity
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
                output = func(entry);
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

        private static T CreateCopyFromPropertyValues<T>(EntityEntry<T> entry, PropertyValues values) where T : BaseEntity
        {
            if (values == null)
                return null;

            var copy = (T)Activator.CreateInstance(typeof(T));
            foreach (var property in entry.Properties)
            {
                var propertyName = property.Metadata.Name;
                var propertyInfo = typeof(T).GetProperty(propertyName);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    var value = values[propertyName];
                    propertyInfo.SetValue(copy, value);
                }
            }
            return copy;
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
            return InnerGetCopy(context, currentCopy, entry =>
            {
                var originalValues = entry.OriginalValues;
                return CreateCopyFromPropertyValues(entry, originalValues);
            });
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
            return InnerGetCopy(context, currentCopy, entry =>
            {
                var databaseValues = entry.GetDatabaseValues();
                return CreateCopyFromPropertyValues(entry, databaseValues);
            });
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
            var tableExists = context.Database
                .SqlQueryRaw<int>("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", tableName)
                .ToList();

            if (tableExists.Any())
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
            var dbContext = CastOrThrow(context);
            var entType = FindEntityTypeByName(dbContext, entityTypeName);
            if (entType == null)
                return null;

            var property = entType.FindProperty(columnName);
            if (property == null)
                return null;

            return property.GetMaxLength();
        }

        /// <summary>
        /// Get columns maximum length
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="entityTypeName">Entity type name</param>
        /// <param name="columnNames">Column names</param>
        /// <returns></returns>
        public static System.Collections.Generic.IDictionary<string, int> GetColumnsMaxLength(this IDbContext context, string entityTypeName, params string[] columnNames)
        {
            var result = new System.Collections.Generic.Dictionary<string, int>();
            var dbContext = CastOrThrow(context);
            var entType = FindEntityTypeByName(dbContext, entityTypeName);
            if (entType == null)
                return result;

            foreach (var columnName in columnNames)
            {
                var property = entType.FindProperty(columnName);
                if (property != null)
                {
                    var maxLength = property.GetMaxLength();
                    if (maxLength.HasValue)
                        result[columnName] = maxLength.Value;
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
        public static System.Collections.Generic.IDictionary<string, decimal> GetDecimalMaxValue(this IDbContext context, string entityTypeName, params string[] columnNames)
        {
            var result = new System.Collections.Generic.Dictionary<string, decimal>();
            var dbContext = CastOrThrow(context);
            var entType = FindEntityTypeByName(dbContext, entityTypeName);
            if (entType == null)
                return result;

            foreach (var columnName in columnNames)
            {
                var property = entType.FindProperty(columnName);
                if (property != null)
                {
                    var precision = property.GetPrecision();
                    var scale = property.GetScale();
                    if (precision.HasValue && scale.HasValue)
                    {
                        var maxValue = (decimal)Math.Pow(10, precision.Value - scale.Value);
                        result[columnName] = maxValue;
                    }
                }
            }

            return result;
        }

        public static string DbName(this IDbContext context)
        {
            var dbContext = CastOrThrow(context);
            var connection = dbContext.Database.GetDbConnection();
            return connection.Database ?? string.Empty;
        }

        private static Microsoft.EntityFrameworkCore.Metadata.IEntityType FindEntityTypeByName(DbContext dbContext, string entityTypeName)
        {
            // Try direct CLR type lookup
            var type = Type.GetType(entityTypeName);
            if (type != null)
            {
                return dbContext.Model.FindEntityType(type);
            }

            // Fallback: match by name
            foreach (var entityType in dbContext.Model.GetEntityTypes())
            {
                if (entityType.ClrType.Name == entityTypeName || entityType.ClrType.FullName == entityTypeName)
                    return entityType;
            }

            return null;
        }

        #endregion
    }
}
