using Microsoft.EntityFrameworkCore;

namespace Nop.Data.Initializers
{
    /// <summary>
    /// Replacement for EF6's <c>System.Data.Entity.IDatabaseInitializer&lt;TContext&gt;</c>.
    /// </summary>
    /// <remarks>
    /// EF6 -> EF Core port (task 3.2). EF6 had a global initializer hook
    /// (<c>Database.SetInitializer(...)</c>) that the framework invoked lazily the first time a
    /// context was used. EF Core removed the concept entirely: schema creation is an explicit
    /// action (<c>Database.EnsureCreated()</c>, <c>Database.Migrate()</c>, or executing a
    /// generated script), performed by the application at a point of its choosing.
    ///
    /// This interface keeps the initializer as a first-class, *explicitly invoked* object so the
    /// nopCommerce installation path can keep its existing structure. It is no longer wired into
    /// EF, so nothing calls <see cref="InitializeDatabase"/> implicitly - see the runtime-deferrals
    /// register.
    /// </remarks>
    /// <typeparam name="TContext">Context type</typeparam>
    public interface INopDatabaseInitializer<in TContext> where TContext : DbContext
    {
        /// <summary>
        /// Create/validate the database schema for the supplied context.
        /// </summary>
        /// <param name="context">Context</param>
        void InitializeDatabase(TContext context);
    }
}
