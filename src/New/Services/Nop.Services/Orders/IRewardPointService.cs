using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface IRewardPointService
{
    Task<IPagedList<RewardPointsHistory>> GetRewardPointsHistoryAsync(int customerId = 0, int storeId = 0,
        bool showNotActivated = false, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<int> AddRewardPointsHistoryEntryAsync(Customer customer, int points, int storeId,
        string message = "", int? usedWithOrderId = null, decimal usedAmount = 0m,
        DateTime? activatingDate = null);

    Task<int> GetRewardPointsBalanceAsync(int customerId, int storeId);

    Task<RewardPointsHistory?> GetRewardPointsHistoryEntryByIdAsync(int rewardPointsHistoryId);

    Task DeleteRewardPointsHistoryEntryAsync(RewardPointsHistory rewardPointsHistory);

    Task UpdateRewardPointsHistoryEntryAsync(RewardPointsHistory rewardPointsHistory);
}
