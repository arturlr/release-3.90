using Nop.Core;
using Nop.Core.Domain.Affiliates;

namespace Nop.Services.Affiliates;

public interface IAffiliateService
{
    Task<Affiliate?> GetAffiliateByIdAsync(int affiliateId);

    Task<Affiliate?> GetAffiliateByFriendlyUrlNameAsync(string friendlyUrlName);

    Task DeleteAffiliateAsync(Affiliate affiliate);

    Task<IPagedList<Affiliate>> GetAllAffiliatesAsync(
        string? friendlyUrlName = null,
        string? firstName = null,
        string? lastName = null,
        bool loadOnlyWithOrders = false,
        DateTime? ordersCreatedFromUtc = null,
        DateTime? ordersCreatedToUtc = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false);

    Task InsertAffiliateAsync(Affiliate affiliate);

    Task UpdateAffiliateAsync(Affiliate affiliate);
}
