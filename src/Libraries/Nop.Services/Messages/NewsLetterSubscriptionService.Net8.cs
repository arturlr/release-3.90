using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Nop.Core.Domain.Messages;
using Nop.Data;

namespace Nop.Services.Messages
{
    public class NewsLetterSubscriptionService : INewsLetterSubscriptionService
    {
        private readonly IRepository<NewsLetterSubscription> _subscriptionRepository;

        public NewsLetterSubscriptionService(IRepository<NewsLetterSubscription> subscriptionRepository)
        {
            _subscriptionRepository = subscriptionRepository;
        }

        public virtual async Task<NewsLetterSubscription> GetNewsLetterSubscriptionByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            return await _subscriptionRepository.Table
                .FirstOrDefaultAsync(s => s.Email == email);
        }

        public virtual async Task<IList<NewsLetterSubscription>> GetAllNewsLetterSubscriptionsAsync()
        {
            var query = _subscriptionRepository.Table
                .Where(s => s.Active)
                .OrderByDescending(s => s.CreatedOnUtc);

            return await query.ToListAsync();
        }

        public virtual async Task InsertNewsLetterSubscriptionAsync(NewsLetterSubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _subscriptionRepository.InsertAsync(subscription);
        }

        public virtual async Task UpdateNewsLetterSubscriptionAsync(NewsLetterSubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _subscriptionRepository.UpdateAsync(subscription);
        }

        public virtual async Task DeleteNewsLetterSubscriptionAsync(NewsLetterSubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _subscriptionRepository.DeleteAsync(subscription);
        }
    }
}
