using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Services.Events;

namespace Nop.Services.Messages;

public class NewsLetterSubscriptionService(
    IRepository<NewsLetterSubscription> subscriptionRepository,
    IRepository<Customer> customerRepository,
    IRepository<CustomerCustomerRoleMapping> customerRoleMappingRepository,
    IRepository<CustomerRole> customerRoleRepository,
    IEventPublisher eventPublisher) : INewsLetterSubscriptionService
{
    public Task<NewsLetterSubscription?> GetNewsLetterSubscriptionByIdAsync(int newsLetterSubscriptionId)
    {
        if (newsLetterSubscriptionId == 0)
            return Task.FromResult<NewsLetterSubscription?>(null);

        return Task.FromResult<NewsLetterSubscription?>(subscriptionRepository.GetById(newsLetterSubscriptionId));
    }

    public Task<NewsLetterSubscription?> GetNewsLetterSubscriptionByGuidAsync(Guid newsLetterSubscriptionGuid)
    {
        if (newsLetterSubscriptionGuid == Guid.Empty)
            return Task.FromResult<NewsLetterSubscription?>(null);

        var result = subscriptionRepository.Table
            .FirstOrDefault(nls => nls.NewsLetterSubscriptionGuid == newsLetterSubscriptionGuid);
        return Task.FromResult(result);
    }

    public Task<NewsLetterSubscription?> GetNewsLetterSubscriptionByEmailAndStoreIdAsync(string email, int storeId)
    {
        if (!CommonHelper.IsValidEmail(email))
            return Task.FromResult<NewsLetterSubscription?>(null);

        email = email.Trim();
        var result = subscriptionRepository.Table
            .FirstOrDefault(nls => nls.Email == email && nls.StoreId == storeId);
        return Task.FromResult(result);
    }

    public Task<IPagedList<NewsLetterSubscription>> GetAllNewsLetterSubscriptionsAsync(
        string? email = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        int storeId = 0, bool? isActive = null, int customerRoleId = 0,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = subscriptionRepository.Table;

        if (!string.IsNullOrEmpty(email))
            query = query.Where(nls => nls.Email!.Contains(email));
        if (createdFromUtc.HasValue)
            query = query.Where(nls => nls.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue)
            query = query.Where(nls => nls.CreatedOnUtc <= createdToUtc.Value);
        if (storeId > 0)
            query = query.Where(nls => nls.StoreId == storeId);
        if (isActive.HasValue)
            query = query.Where(nls => nls.Active == isActive.Value);

        if (customerRoleId > 0)
        {
            // check if this is the guest role
            var guestRole = customerRoleRepository.Table
                .FirstOrDefault(cr => cr.SystemName == SystemCustomerRoleNames.Guests);

            if (guestRole != null && guestRole.Id == customerRoleId)
            {
                // guests = subscriptions where email doesn't match any customer
                query = query.Where(nls => !customerRepository.Table.Any(c => c.Email == nls.Email));
            }
            else
            {
                // filter by customer role via join
                query = from nls in query
                        join c in customerRepository.Table on nls.Email equals c.Email
                        join crm in customerRoleMappingRepository.Table on c.Id equals crm.CustomerId
                        where crm.CustomerRoleId == customerRoleId
                        select nls;
            }
        }

        query = query.OrderBy(nls => nls.Email);
        return Task.FromResult<IPagedList<NewsLetterSubscription>>(new PagedList<NewsLetterSubscription>(query, pageIndex, pageSize));
    }

    public async Task InsertNewsLetterSubscriptionAsync(NewsLetterSubscription newsLetterSubscription, bool publishSubscriptionEvents = true)
    {
        ArgumentNullException.ThrowIfNull(newsLetterSubscription);
        newsLetterSubscription.Email = CommonHelper.EnsureSubscriberEmailOrThrow(newsLetterSubscription.Email!);

        subscriptionRepository.Insert(newsLetterSubscription);

        if (newsLetterSubscription.Active && publishSubscriptionEvents)
            await eventPublisher.PublishAsync(new EmailSubscribedEvent(newsLetterSubscription));

        await eventPublisher.EntityInsertedAsync(newsLetterSubscription);
    }

    public async Task UpdateNewsLetterSubscriptionAsync(NewsLetterSubscription newsLetterSubscription, bool publishSubscriptionEvents = true)
    {
        ArgumentNullException.ThrowIfNull(newsLetterSubscription);
        newsLetterSubscription.Email = CommonHelper.EnsureSubscriberEmailOrThrow(newsLetterSubscription.Email!);

        // snapshot original values before update (no IDbContext.LoadOriginalCopy — query by id)
        var original = subscriptionRepository.TableNoTracking
            .FirstOrDefault(nls => nls.Id == newsLetterSubscription.Id);

        subscriptionRepository.Update(newsLetterSubscription);

        if (publishSubscriptionEvents && original != null)
        {
            // was inactive, now active → subscribe
            if (!original.Active && newsLetterSubscription.Active)
                await eventPublisher.PublishAsync(new EmailSubscribedEvent(newsLetterSubscription));

            // was active, email changed → subscribe new + unsubscribe old
            if (original.Active && newsLetterSubscription.Active && original.Email != newsLetterSubscription.Email)
            {
                await eventPublisher.PublishAsync(new EmailSubscribedEvent(newsLetterSubscription));
                await eventPublisher.PublishAsync(new EmailUnsubscribedEvent(original));
            }

            // was active, now inactive → unsubscribe
            if (original.Active && !newsLetterSubscription.Active)
                await eventPublisher.PublishAsync(new EmailUnsubscribedEvent(original));
        }

        await eventPublisher.EntityUpdatedAsync(newsLetterSubscription);
    }

    public async Task DeleteNewsLetterSubscriptionAsync(NewsLetterSubscription newsLetterSubscription, bool publishSubscriptionEvents = true)
    {
        ArgumentNullException.ThrowIfNull(newsLetterSubscription);

        subscriptionRepository.Delete(newsLetterSubscription);

        if (publishSubscriptionEvents)
            await eventPublisher.PublishAsync(new EmailUnsubscribedEvent(newsLetterSubscription));

        await eventPublisher.EntityDeletedAsync(newsLetterSubscription);
    }
}
