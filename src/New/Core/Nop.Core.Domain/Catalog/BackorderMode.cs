namespace Nop.Core.Domain.Catalog
{

    public enum BackorderMode
    {

        NoBackorders = 0,

        AllowQtyBelow0 = 1,

        AllowQtyBelow0AndNotifyCustomer = 2,
    }
}
