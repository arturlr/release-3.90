using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;
using Nop.Services.Messages;

namespace Nop.Services.Catalog;

public class BackInStockSubscriptionService : IBackInStockSubscriptionService
{
    private readonly IRepository<BackInStockSubscription> _backInStockSubscriptionRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IWorkflowMessageService _workflowMessageService;

    public BackInStockSubscriptionService(
        IRepository<BackInStockSubscription> backInStockSubscriptionRepository,
        IEventPublisher eventPublisher,
        IWorkflowMessageService workflowMessageService)
    {
        _backInStockSubscriptionRepository = backInStockSubscriptionRepository;
        _eventPublisher = eventPublisher;
        _workflowMessageService = workflowMessageService;
    }

    public virtual async Task DeleteSubscriptionAsync(BackInStockSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        _backInStockSubscriptionRepository.Delete(subscription);
        await _eventPublisher.EntityDeletedAsync(subscription);
    }

    public virtual Task<IPagedList<BackInStockSubscription>> GetAllSubscriptionsByCustomerIdAsync(int customerId, int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _backInStockSubscriptionRepository.TableNoTracking
            .Where(s => s.CustomerId == customerId);
        if (storeId > 0)
            query = query.Where(s => s.StoreId == storeId);
        query = query.OrderByDescending(s => s.CreatedOnUtc);
        return Task.FromResult<IPagedList<BackInStockSubscription>>(new PagedList<BackInStockSubscription>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<IPagedList<BackInStockSubscription>> GetAllSubscriptionsByProductIdAsync(int productId, int storeId = 0, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = _backInStockSubscriptionRepository.TableNoTracking
            .Where(s => s.ProductId == productId);
        if (storeId > 0)
            query = query.Where(s => s.StoreId == storeId);
        query = query.OrderByDescending(s => s.CreatedOnUtc);
        return Task.FromResult<IPagedList<BackInStockSubscription>>(new PagedList<BackInStockSubscription>(query.ToList(), pageIndex, pageSize));
    }

    public virtual Task<BackInStockSubscription?> FindSubscriptionAsync(int customerId, int productId, int storeId)
    {
        var subscription = _backInStockSubscriptionRepository.TableNoTracking
            .FirstOrDefault(s => s.CustomerId == customerId && s.ProductId == productId && s.StoreId == storeId);
        return Task.FromResult(subscription);
    }

    public virtual Task<BackInStockSubscription?> GetSubscriptionByIdAsync(int subscriptionId) =>
        Task.FromResult(subscriptionId == 0 ? null : _backInStockSubscriptionRepository.GetById(subscriptionId));

    public virtual async Task InsertSubscriptionAsync(BackInStockSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        _backInStockSubscriptionRepository.Insert(subscription);
        await _eventPublisher.EntityInsertedAsync(subscription);
    }

    public virtual async Task UpdateSubscriptionAsync(BackInStockSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        _backInStockSubscriptionRepository.Update(subscription);
        await _eventPublisher.EntityUpdatedAsync(subscription);
    }

    public virtual async Task<int> SendNotificationsToSubscribersAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        var subscriptions = _backInStockSubscriptionRepository.Table
            .Where(s => s.ProductId == product.Id).ToList();

        var sent = 0;
        foreach (var subscription in subscriptions)
        {
            await _workflowMessageService.SendBackInStockNotificationAsync(subscription, 0);
            await DeleteSubscriptionAsync(subscription);
            sent++;
        }

        return sent;
    }
}
