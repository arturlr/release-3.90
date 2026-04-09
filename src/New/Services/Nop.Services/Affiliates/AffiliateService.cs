using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Affiliates;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Orders;
using Nop.Services.Events;

namespace Nop.Services.Affiliates;

public class AffiliateService : IAffiliateService
{
    private readonly IRepository<Affiliate> _affiliateRepository;
    private readonly IRepository<Address> _addressRepository;
    private readonly IRepository<Order> _orderRepository;
    private readonly IEventPublisher _eventPublisher;

    public AffiliateService(
        IRepository<Affiliate> affiliateRepository,
        IRepository<Address> addressRepository,
        IRepository<Order> orderRepository,
        IEventPublisher eventPublisher)
    {
        _affiliateRepository = affiliateRepository;
        _addressRepository = addressRepository;
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
    }

    public Task<Affiliate?> GetAffiliateByIdAsync(int affiliateId)
    {
        return Task.FromResult(affiliateId == 0 ? null : _affiliateRepository.GetById(affiliateId));
    }

    public Task<Affiliate?> GetAffiliateByFriendlyUrlNameAsync(string friendlyUrlName)
    {
        if (string.IsNullOrWhiteSpace(friendlyUrlName))
            return Task.FromResult<Affiliate?>(null);

        var affiliate = _affiliateRepository.Table
            .Where(a => a.FriendlyUrlName == friendlyUrlName)
            .OrderBy(a => a.Id)
            .FirstOrDefault();

        return Task.FromResult<Affiliate?>(affiliate);
    }

    public async Task DeleteAffiliateAsync(Affiliate affiliate)
    {
        ArgumentNullException.ThrowIfNull(affiliate);

        affiliate.Deleted = true;
        _affiliateRepository.Update(affiliate);

        await _eventPublisher.EntityDeletedAsync(affiliate);
    }

    public Task<IPagedList<Affiliate>> GetAllAffiliatesAsync(
        string? friendlyUrlName = null,
        string? firstName = null,
        string? lastName = null,
        bool loadOnlyWithOrders = false,
        DateTime? ordersCreatedFromUtc = null,
        DateTime? ordersCreatedToUtc = null,
        int pageIndex = 0,
        int pageSize = int.MaxValue,
        bool showHidden = false)
    {
        var query = _affiliateRepository.Table.Where(a => !a.Deleted);

        if (!string.IsNullOrWhiteSpace(friendlyUrlName))
            query = query.Where(a => a.FriendlyUrlName != null && a.FriendlyUrlName.Contains(friendlyUrlName));

        if (!showHidden)
            query = query.Where(a => a.Active);

        // firstName/lastName filtering requires join to Address (no nav properties)
        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
        {
            var addresses = _addressRepository.TableNoTracking;
            query = from a in query
                    join addr in addresses on a.AddressId equals addr.Id
                    where (string.IsNullOrWhiteSpace(firstName) || (addr.FirstName != null && addr.FirstName.Contains(firstName)))
                       && (string.IsNullOrWhiteSpace(lastName) || (addr.LastName != null && addr.LastName.Contains(lastName)))
                    select a;
        }

        if (loadOnlyWithOrders)
        {
            var ordersQuery = _orderRepository.TableNoTracking.Where(o => !o.Deleted);
            if (ordersCreatedFromUtc.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedOnUtc >= ordersCreatedFromUtc.Value);
            if (ordersCreatedToUtc.HasValue)
                ordersQuery = ordersQuery.Where(o => o.CreatedOnUtc <= ordersCreatedToUtc.Value);

            query = from a in query
                    where ordersQuery.Any(o => o.AffiliateId == a.Id)
                    select a;
        }

        query = query.OrderByDescending(a => a.Id);

        IPagedList<Affiliate> result = new PagedList<Affiliate>(query, pageIndex, pageSize);
        return Task.FromResult(result);
    }

    public async Task InsertAffiliateAsync(Affiliate affiliate)
    {
        ArgumentNullException.ThrowIfNull(affiliate);

        _affiliateRepository.Insert(affiliate);

        await _eventPublisher.EntityInsertedAsync(affiliate);
    }

    public async Task UpdateAffiliateAsync(Affiliate affiliate)
    {
        ArgumentNullException.ThrowIfNull(affiliate);

        _affiliateRepository.Update(affiliate);

        await _eventPublisher.EntityUpdatedAsync(affiliate);
    }
}
