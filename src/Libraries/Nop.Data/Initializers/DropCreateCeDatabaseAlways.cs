using System;
using Microsoft.EntityFrameworkCore;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// An implementation that will always recreate and optionally re-seed the
    /// database the first time that a context is used in the app domain.
    /// Adapted for EF Core - SQL Server Compact is not supported in .NET Core.
    /// </summary>
    /// <typeparam name="TContext">The type of the context.</typeparam>
    public class DropCreateCeDatabaseAlways<TContext> : SqlCeInitializer<TContext> where TContext : DbContext
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

            if (context.Database.CanConnect())
            {
                context.Database.EnsureDeleted();
            }
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
