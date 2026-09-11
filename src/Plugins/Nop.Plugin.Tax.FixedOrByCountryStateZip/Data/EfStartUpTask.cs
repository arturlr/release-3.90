using Nop.Core.Infrastructure;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Data
{
    /// <summary>
    /// Startup task for this plugin's data context.
    /// </summary>
    /// <remarks>
    /// Task 15.1 — EF6 → EF Core. 3.90's body was
    /// <c>Database.SetInitializer&lt;CountryStateZipObjectContext&gt;(null)</c>, which suppressed
    /// EF6's model-compatibility initializer so the "model backing the context has changed" check
    /// would not run. <c>System.Data.Entity.Database.SetInitializer</c> is an EF6 static with
    /// <b>no EF Core counterpart</b>: EF Core has no database initializers and never runs that
    /// check, so there is nothing to disable. The body is therefore a deliberate no-op.
    /// <para>
    /// The <see cref="IStartupTask"/> itself is KEPT rather than deleted so that Nop.Core's
    /// startup-task enumeration (which reflects over every <c>IStartupTask</c> in every loaded
    /// assembly) sees exactly the set of tasks it did in 3.90 — one fewer task would be a silent
    /// change to that surface. <c>Order</c> is 3.90's.
    /// </para>
    /// </remarks>
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            //EF6: Database.SetInitializer<CountryStateZipObjectContext>(null);
            //EF Core has no database initializers, so there is nothing to disable here.
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
