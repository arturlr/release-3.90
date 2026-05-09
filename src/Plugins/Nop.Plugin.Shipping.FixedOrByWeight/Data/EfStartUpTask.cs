using Nop.Core.Infrastructure;

namespace Nop.Plugin.Shipping.FixedOrByWeight.Data
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
            //ensure that this task is run first 
            get { return 0; }
        }
    }
}
