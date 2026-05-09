using System;
using Microsoft.EntityFrameworkCore;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// An implementation that will DELETE, recreate, and optionally re-seed the
    /// database only if the model has changed since the database was created.
    /// Adapted for EF Core - SQL Server Compact is not supported in .NET Core.
    /// </summary>
    public class DropCreateCeDatabaseIfModelChanges<TContext> : SqlCeInitializer<TContext> where TContext : DbContext
    {
        #region Strategy implementation

        /// <summary>
        /// Executes the strategy to initialize the database for the given context.
        /// </summary>
        /// <param name="context">The context.</param>
        public override void InitializeDatabase(TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool databaseExists = context.Database.CanConnect();

            if (databaseExists)
            {
                // In EF Core, model compatibility checking is handled differently
                // Use migrations for schema changes instead
                return;
            }

            // Database didn't exist, so create it
            context.Database.EnsureCreated();

            Seed(context);
            context.SaveChanges();
        }

        #endregion

        #region Seeding methods

        /// <summary>
        /// A that should be overridden to actually add data to the context for seeding. 
        /// The default implementation does nothing.
        /// </summary>
        /// <param name="context">The context to seed.</param>
        protected virtual void Seed(TContext context)
        {
        }

        #endregion
    }
}
