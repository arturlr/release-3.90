using Nop.Core.Domain.Stores;

namespace Nop.Core;

/// <summary>
/// Store context — provides the current store for multi-store setups
/// </summary>
public interface IStoreContext
{
    Store CurrentStore { get; }
}
