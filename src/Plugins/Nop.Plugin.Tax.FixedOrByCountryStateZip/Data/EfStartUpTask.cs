using Nop.Core.Infrastructure;

namespace Nop.Plugin.Tax.FixedOrByCountryStateZip.Data
{
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            // In EF Core, database initialization is handled differently.
            // No initializer needs to be set - EF Core does not use database initializers.
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
