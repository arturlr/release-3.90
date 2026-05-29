using Nop.Core.Infrastructure;

namespace Nop.Plugin.Shipping.FixedOrByWeight.Data
{
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            // EF Core handles migrations differently - no initializer needed
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
