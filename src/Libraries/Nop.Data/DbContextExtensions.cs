using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Nop.Core;

namespace Nop.Data
{
    public static class DbContextExtensions
    {
        #region Methods

        /// <summary>
        /// Loads the original copy of an entity from the change tracker
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The context</param>
        /// <param name="currentCopy">The current copy</param>
        /// <returns>Original values entity</returns>
        public static T LoadOriginalCopy<T>(this IDbContext context, T currentCopy) where T : BaseEntity
        {
            var dbContext = CastOrThrow(context);
            var entry = dbContext.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == currentCopy);

            if (entry == null)
                return null;

            var originalValues = entry.OriginalValues;
            var clone = (T)Activator.CreateInstance(typeof(T));

            foreach (var property in originalValues.Properties)
            {
                var propertyInfo = typeof(T).GetProperty(property.Name);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(clone, originalValues[property]);
                }
            }

            return clone;
        }

        /// <summary>
        /// Loads the database copy of an entity (queries DB directly)
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="context">The context</param>
        /// <param name="currentCopy">The current copy</param>
        /// <returns>Database values entity</returns>
        public static T LoadDatabaseCopy<T>(this IDbContext context, T currentCopy) where T : BaseEntity
        {
            var dbContext = CastOrThrow(context);
            var entry = dbContext.ChangeTracker.Entries<T>().FirstOrDefault(e => e.Entity == currentCopy);

            if (entry == null)
                return null;

            var databaseValues = entry.GetDatabaseValues();
            if (databaseValues == null)
                return null;

            var clone = (T)Activator.CreateInstance(typeof(T));
            foreach (var property in databaseValues.Properties)
            {
                var propertyInfo = typeof(T).GetProperty(property.Name);
                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    propertyInfo.SetValue(clone, databaseValues[property]);
                }
            }

            return clone;
        }

        /// <summary>
        /// Drop a plugin table
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="tableName">Table name</param>
        public static void DropPluginTable(this DbContext context, string tableName)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(nameof(tableName));

            // Check if table exists then drop it
            var tableExists = context.Database
                .SqlQueryRaw<int>("SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = {0}", tableName)
                .Any();

            if (tableExists)
            {
                context.Database.ExecuteSqlRaw($"DROP TABLE [{tableName}]");
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
            return entityType?.GetTableName() ?? typeof(T).Name;
        }

        /// <summary>
        /// Get database name
        /// </summary>
        /// <param name="context">Context</param>
        /// <returns>Database name</returns>
        public static string DbName(this IDbContext context)
        {
            var dbContext = CastOrThrow(context);
            return dbContext.Database.GetDbConnection().Database;
        }

        #endregion

        #region Utilities

        private static DbContext CastOrThrow(IDbContext context)
        {
            var output = context as DbContext;
            if (output == null)
                throw new InvalidOperationException("Context does not support operation.");
            return output;
        }

        #endregion
    }
}
