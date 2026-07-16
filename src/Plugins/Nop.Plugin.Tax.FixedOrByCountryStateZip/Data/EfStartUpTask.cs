using Nop.Core.Infrastructure;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Data
{
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            // EF Core does not use Database.SetInitializer.
            // No initialization needed for EF Core.
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
