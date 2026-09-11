using Nop.Core.Infrastructure;

namespace Nop.Plugin.Pickup.PickupInStore.Data
{
    /// <summary>
    /// Startup task for this plugin's data context.
    /// </summary>
    /// <remarks>
    /// Task 13.1 — this class has NO behavioural counterpart under EF Core, and that is faithful,
    /// not an omission.
    ///
    /// 3.90's body was <c>Database.SetInitializer&lt;StorePickupPointObjectContext&gt;(null)</c>.
    /// <c>System.Data.Entity.Database.SetInitializer</c> registered EF6's global, lazily-fired
    /// database initializer for a context type; passing <c>null</c> DISABLED it, so EF6 would not
    /// try to recreate or migrate the schema on first use when the model appeared to have changed
    /// (the "model backing the context has changed" exception the old comment described).
    ///
    /// EF Core removed the entire initializer subsystem: there is no <c>Database.SetInitializer</c>,
    /// no <c>IDatabaseInitializer</c>, and EF Core NEVER auto-creates or auto-migrates a schema on
    /// model drift — schema creation is explicit (this plugin does it in
    /// <c>StorePickupPointObjectContext.Install</c> via <c>ExecuteSqlScript</c>). So there is nothing
    /// to disable, and the 3.90 statement has no EF Core equivalent to translate to. See
    /// <c>Nop.Data</c>'s own initializer handling, runtime-deferrals.md §4.8.
    ///
    /// The <see cref="IStartupTask"/> type is KEPT (rather than deleted) so the plugin's
    /// startup-task surface is unchanged and the discovered-type set stays identical to 3.90's; the
    /// body is now an intentional no-op.
    /// </remarks>
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            //EF Core has no database initializer to disable - see the class remarks. No-op.
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
