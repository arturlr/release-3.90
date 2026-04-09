using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Messages;
using Nop.Services.Events;

namespace Nop.Services.Messages;

public class CampaignService(
    IRepository<Campaign> campaignRepository,
    IRepository<Customer> customerRepository,
    IEmailSender emailSender,
    IMessageTokenProvider messageTokenProvider,
    ITokenizer tokenizer,
    IQueuedEmailService queuedEmailService,
    IStoreContext storeContext,
    IEventPublisher eventPublisher) : ICampaignService
{
    public Task<Campaign?> GetCampaignByIdAsync(int campaignId)
    {
        if (campaignId == 0)
            return Task.FromResult<Campaign?>(null);
        return Task.FromResult<Campaign?>(campaignRepository.GetById(campaignId));
    }

    public Task<IList<Campaign>> GetAllCampaignsAsync(int storeId = 0)
    {
        var query = campaignRepository.Table;
        if (storeId > 0)
            query = query.Where(c => c.StoreId == storeId);
        query = query.OrderBy(c => c.CreatedOnUtc);
        return Task.FromResult<IList<Campaign>>(query.ToList());
    }

    public async Task InsertCampaignAsync(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        campaignRepository.Insert(campaign);
        await eventPublisher.EntityInsertedAsync(campaign);
    }

    public async Task UpdateCampaignAsync(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        campaignRepository.Update(campaign);
        await eventPublisher.EntityUpdatedAsync(campaign);
    }

    public async Task DeleteCampaignAsync(Campaign campaign)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        campaignRepository.Delete(campaign);
        await eventPublisher.EntityDeletedAsync(campaign);
    }

    public async Task<int> SendCampaignAsync(Campaign campaign, EmailAccount emailAccount, IEnumerable<NewsLetterSubscription> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(emailAccount);

        var totalSent = 0;
        foreach (var subscription in subscriptions)
        {
            var customer = customerRepository.Table.FirstOrDefault(c => c.Email == subscription.Email);
            // skip deleted or inactive customers
            if (customer is { Active: false } or { Deleted: true })
                continue;

            var tokens = new List<Token>();
            await messageTokenProvider.AddStoreTokensAsync(tokens, storeContext.CurrentStore, emailAccount);
            await messageTokenProvider.AddNewsLetterSubscriptionTokensAsync(tokens, subscription);
            if (customer != null)
                await messageTokenProvider.AddCustomerTokensAsync(tokens, customer);

            var subject = tokenizer.Replace(campaign.Subject ?? string.Empty, tokens, false);
            var body = tokenizer.Replace(campaign.Body ?? string.Empty, tokens, true);

            var queuedEmail = new QueuedEmail
            {
                PriorityId = (int)QueuedEmailPriority.Low,
                From = emailAccount.Email,
                FromName = emailAccount.DisplayName,
                To = subscription.Email,
                Subject = subject,
                Body = body,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = emailAccount.Id,
                DontSendBeforeDateUtc = campaign.DontSendBeforeDateUtc
            };
            await queuedEmailService.InsertQueuedEmailAsync(queuedEmail);
            totalSent++;
        }
        return totalSent;
    }

    public async Task SendCampaignAsync(Campaign campaign, EmailAccount emailAccount, string email)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(emailAccount);

        var tokens = new List<Token>();
        await messageTokenProvider.AddStoreTokensAsync(tokens, storeContext.CurrentStore, emailAccount);
        var customer = customerRepository.Table.FirstOrDefault(c => c.Email == email);
        if (customer != null)
            await messageTokenProvider.AddCustomerTokensAsync(tokens, customer);

        var subject = tokenizer.Replace(campaign.Subject ?? string.Empty, tokens, false);
        var body = tokenizer.Replace(campaign.Body ?? string.Empty, tokens, true);

        await emailSender.SendEmailAsync(emailAccount, subject, body,
            emailAccount.Email!, emailAccount.DisplayName!, email, string.Empty);
    }
}
