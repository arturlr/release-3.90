using System.Collections.Generic;
using System.Threading.Tasks;
using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages
{
    public interface INewsLetterSubscriptionService
    {
        Task<NewsLetterSubscription> GetNewsLetterSubscriptionByEmailAsync(string email);
        Task<IList<NewsLetterSubscription>> GetAllNewsLetterSubscriptionsAsync();
        Task InsertNewsLetterSubscriptionAsync(NewsLetterSubscription subscription);
        Task UpdateNewsLetterSubscriptionAsync(NewsLetterSubscription subscription);
        Task DeleteNewsLetterSubscriptionAsync(NewsLetterSubscription subscription);
    }
}
