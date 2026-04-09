using Nop.Core;
using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public interface INewsLetterSubscriptionService
{
    Task<NewsLetterSubscription?> GetNewsLetterSubscriptionByIdAsync(int newsLetterSubscriptionId);
    Task<NewsLetterSubscription?> GetNewsLetterSubscriptionByGuidAsync(Guid newsLetterSubscriptionGuid);
    Task<NewsLetterSubscription?> GetNewsLetterSubscriptionByEmailAndStoreIdAsync(string email, int storeId);
    Task<IPagedList<NewsLetterSubscription>> GetAllNewsLetterSubscriptionsAsync(
        string? email = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int storeId = 0, bool? isActive = null, int customerRoleId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertNewsLetterSubscriptionAsync(NewsLetterSubscription newsLetterSubscription, bool publishSubscriptionEvents = true);
    Task UpdateNewsLetterSubscriptionAsync(NewsLetterSubscription newsLetterSubscription, bool publishSubscriptionEvents = true);
    Task DeleteNewsLetterSubscriptionAsync(NewsLetterSubscription newsLetterSubscription, bool publishSubscriptionEvents = true);
}
