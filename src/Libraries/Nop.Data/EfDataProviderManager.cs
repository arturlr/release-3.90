using System;
using Nop.Core;
using Nop.Core.Data;

namespace Nop.Data
{
    public partial class EfDataProviderManager : BaseDataProviderManager
    {
        public EfDataProviderManager(DataSettings settings):base(settings)
        {
        }

        public override IDataProvider LoadDataProvider()
        {

            var providerName = Settings.DataProvider;
            if (String.IsNullOrWhiteSpace(providerName))
                throw new NopException("Data Settings doesn't contain a providerName");

            switch (providerName.ToLowerInvariant())
            {
                case "sqlserver":
                    return new SqlServerDataProvider();
                //SQL Server Compact ("sqlce") was removed in the .NET 10 migration:
                //EF Core has no SQL CE provider, so SqlCeDataProvider no longer exists.
                //A DataSettings file still naming "sqlce" now falls through to the
                //NopException below rather than silently binding a dead provider.
                default:
                    throw new NopException(string.Format("Not supported dataprovider name: {0}", providerName));
            }
        }

    }
}
