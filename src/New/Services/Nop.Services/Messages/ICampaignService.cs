using Nop.Core.Domain.Messages;

namespace Nop.Services.Messages;

public interface ICampaignService
{
    Task<Campaign?> GetCampaignByIdAsync(int campaignId);
    Task<IList<Campaign>> GetAllCampaignsAsync(int storeId = 0);
    Task InsertCampaignAsync(Campaign campaign);
    Task UpdateCampaignAsync(Campaign campaign);
    Task DeleteCampaignAsync(Campaign campaign);
    Task<int> SendCampaignAsync(Campaign campaign, EmailAccount emailAccount, IEnumerable<NewsLetterSubscription> subscriptions);
    Task SendCampaignAsync(Campaign campaign, EmailAccount emailAccount, string email);
}
