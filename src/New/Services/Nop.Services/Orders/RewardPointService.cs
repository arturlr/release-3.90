using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Orders;

public class RewardPointService(
    IRepository<RewardPointsHistory> rphRepository,
    RewardPointsSettings rewardPointsSettings,
    IEventPublisher eventPublisher) : IRewardPointService
{
    public Task<IPagedList<RewardPointsHistory>> GetRewardPointsHistoryAsync(int customerId = 0, int storeId = 0,
        bool showNotActivated = false, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = rphRepository.Table;

        if (customerId > 0)
            query = query.Where(rph => rph.CustomerId == customerId);

        if (storeId > 0 && !rewardPointsSettings.PointsAccumulatedForAllStores)
            query = query.Where(rph => rph.StoreId == storeId);

        if (!showNotActivated)
            query = query.Where(rph => rph.CreatedOnUtc < DateTime.UtcNow);

        // activate pending points
        ActivatePendingPoints(query);

        query = query.OrderByDescending(rph => rph.CreatedOnUtc).ThenByDescending(rph => rph.Id);

        return Task.FromResult<IPagedList<RewardPointsHistory>>(new PagedList<RewardPointsHistory>(query, pageIndex, pageSize));
    }

    public Task<int> AddRewardPointsHistoryEntryAsync(Customer customer, int points, int storeId,
        string message = "", int? usedWithOrderId = null, decimal usedAmount = 0m,
        DateTime? activatingDate = null)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);

        var rph = new RewardPointsHistory
        {
            CustomerId = customer.Id,
            StoreId = storeId,
            Points = points,
            PointsBalance = activatingDate.HasValue ? null : GetRewardPointsBalanceAsync(customer.Id, storeId).GetAwaiter().GetResult() + points,
            UsedAmount = usedAmount,
            Message = message,
            CreatedOnUtc = activatingDate ?? DateTime.UtcNow
        };

        rphRepository.Insert(rph);
        eventPublisher.EntityInsertedAsync(rph).GetAwaiter().GetResult();

        return Task.FromResult(rph.Id);
    }

    public Task<int> GetRewardPointsBalanceAsync(int customerId, int storeId)
    {
        var query = rphRepository.Table;

        if (customerId > 0)
            query = query.Where(rph => rph.CustomerId == customerId);

        if (!rewardPointsSettings.PointsAccumulatedForAllStores)
            query = query.Where(rph => rph.StoreId == storeId);

        query = query.Where(rph => rph.CreatedOnUtc < DateTime.UtcNow);

        ActivatePendingPoints(query);

        var lastRph = query.OrderByDescending(rph => rph.CreatedOnUtc)
            .ThenByDescending(rph => rph.Id)
            .FirstOrDefault();

        return Task.FromResult(lastRph?.PointsBalance ?? 0);
    }

    public Task<RewardPointsHistory?> GetRewardPointsHistoryEntryByIdAsync(int rewardPointsHistoryId)
    {
        if (rewardPointsHistoryId == 0)
            return Task.FromResult<RewardPointsHistory?>(null);

        return Task.FromResult<RewardPointsHistory?>(rphRepository.GetById(rewardPointsHistoryId));
    }

    public Task DeleteRewardPointsHistoryEntryAsync(RewardPointsHistory rewardPointsHistory)
    {
        ArgumentNullException.ThrowIfNull(rewardPointsHistory);
        rphRepository.Delete(rewardPointsHistory);
        return eventPublisher.EntityDeletedAsync(rewardPointsHistory);
    }

    public Task UpdateRewardPointsHistoryEntryAsync(RewardPointsHistory rewardPointsHistory)
    {
        ArgumentNullException.ThrowIfNull(rewardPointsHistory);
        rphRepository.Update(rewardPointsHistory);
        return eventPublisher.EntityUpdatedAsync(rewardPointsHistory);
    }

    private void ActivatePendingPoints(IQueryable<RewardPointsHistory> query)
    {
        var ordered = query.OrderBy(rph => rph.CreatedOnUtc).ThenBy(rph => rph.Id);
        var pending = ordered.Where(rph => !rph.PointsBalance.HasValue && rph.CreatedOnUtc < DateTime.UtcNow).ToList();

        if (pending.Count == 0)
            return;

        var lastActive = ordered.OrderByDescending(rph => rph.CreatedOnUtc)
            .ThenByDescending(rph => rph.Id)
            .FirstOrDefault(rph => rph.PointsBalance.HasValue);

        var balance = lastActive?.PointsBalance ?? 0;

        foreach (var rph in pending)
        {
            balance += rph.Points;
            rph.PointsBalance = balance;
            rphRepository.Update(rph);
        }
    }
}
