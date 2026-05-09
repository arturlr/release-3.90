using System;
using Microsoft.EntityFrameworkCore;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// An implementation that will recreate and optionally re-seed the
    /// database only if the database does not exist.
    /// Adapted for EF Core - SQL Server Compact is not supported in .NET Core.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    public class CreateCeDatabaseIfNotExists<TContext> : SqlCeInitializer<TContext> where TContext : DbContext
    {
        #region Strategy implementation

        public override void InitializeDatabase(TContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            bool databaseExists = context.Database.CanConnect();

            if (databaseExists)
            {
                // Database exists - assume it's compatible
                return;
            }
            else
            {
                context.Database.EnsureCreated();
                Seed(context);
                context.SaveChanges();
            }
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
