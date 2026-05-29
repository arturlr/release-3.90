using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Infrastructure;

namespace Nop.Data
{
    /// <summary>
    /// EF Core startup task - initializes the data provider
    /// </summary>
    public class EfStartUpTask : IStartupTask
    {
        public void Execute()
        {
            var settings = EngineContext.Current.Resolve<DataSettings>();
            if (settings != null && settings.IsValid())
            {
                var provider = EngineContext.Current.Resolve<IDataProvider>();
                if (provider == null)
                    throw new NopException("No IDataProvider found");
                provider.InitDatabase();
            }
        }

        public int Order
        {
            // Ensure that this task is run first
            get { return -1000; }
        }
    }
}
