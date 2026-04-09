using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog;

public interface IBackInStockSubscriptionService
{
    Task DeleteSubscriptionAsync(BackInStockSubscription subscription);
    Task<IPagedList<BackInStockSubscription>> GetAllSubscriptionsByCustomerIdAsync(int customerId, int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<IPagedList<BackInStockSubscription>> GetAllSubscriptionsByProductIdAsync(int productId, int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<BackInStockSubscription?> FindSubscriptionAsync(int customerId, int productId, int storeId);
    Task<BackInStockSubscription?> GetSubscriptionByIdAsync(int subscriptionId);
    Task InsertSubscriptionAsync(BackInStockSubscription subscription);
    Task UpdateSubscriptionAsync(BackInStockSubscription subscription);
    Task<int> SendNotificationsToSubscribersAsync(Product product);
}
