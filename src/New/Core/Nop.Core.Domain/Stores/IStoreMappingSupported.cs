namespace Nop.Core.Domain.Stores
{

    public interface IStoreMappingSupported
    {

        bool LimitedToStores { get; set; }
    }
}
