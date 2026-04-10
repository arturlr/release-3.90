using System.Xml;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Events;

namespace Nop.Services.Orders;

public class GiftCardService(
    IRepository<GiftCard> giftCardRepository,
    IRepository<GiftCardUsageHistory> giftCardUsageHistoryRepository,
    IRepository<OrderItem> orderItemRepository,
    IGenericAttributeService genericAttributeService,
    IEventPublisher eventPublisher) : IGiftCardService
{
    public Task DeleteGiftCardAsync(GiftCard giftCard)
    {
        ArgumentNullException.ThrowIfNull(giftCard);
        giftCardRepository.Delete(giftCard);
        return eventPublisher.EntityDeletedAsync(giftCard);
    }

    public Task<GiftCard?> GetGiftCardByIdAsync(int giftCardId) =>
        Task.FromResult(giftCardId == 0 ? null : (GiftCard?)giftCardRepository.GetById(giftCardId));

    public Task<IPagedList<GiftCard>> GetAllGiftCardsAsync(int? purchasedWithOrderId = null, int? usedWithOrderId = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        bool? isGiftCardActivated = null, string? giftCardCouponCode = null,
        string? recipientName = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var query = giftCardRepository.Table;

        if (purchasedWithOrderId.HasValue)
        {
            // join through OrderItem to find gift cards purchased with a specific order
            query = from gc in query
                    join oi in orderItemRepository.Table on gc.PurchasedWithOrderItemId equals oi.Id
                    where oi.OrderId == purchasedWithOrderId.Value
                    select gc;
        }

        if (usedWithOrderId.HasValue)
        {
            query = from gc in query
                    join gcuh in giftCardUsageHistoryRepository.Table on gc.Id equals gcuh.GiftCardId
                    where gcuh.UsedWithOrderId == usedWithOrderId.Value
                    select gc;
            query = query.Distinct();
        }

        if (createdFromUtc.HasValue) query = query.Where(gc => gc.CreatedOnUtc >= createdFromUtc.Value);
        if (createdToUtc.HasValue) query = query.Where(gc => gc.CreatedOnUtc <= createdToUtc.Value);
        if (isGiftCardActivated.HasValue) query = query.Where(gc => gc.IsGiftCardActivated == isGiftCardActivated.Value);
        if (!string.IsNullOrEmpty(giftCardCouponCode)) query = query.Where(gc => gc.GiftCardCouponCode == giftCardCouponCode);
        if (!string.IsNullOrWhiteSpace(recipientName)) query = query.Where(gc => gc.RecipientName != null && gc.RecipientName.Contains(recipientName));

        query = query.OrderByDescending(gc => gc.CreatedOnUtc);

        return Task.FromResult<IPagedList<GiftCard>>(new PagedList<GiftCard>(query, pageIndex, pageSize));
    }

    public Task InsertGiftCardAsync(GiftCard giftCard)
    {
        ArgumentNullException.ThrowIfNull(giftCard);
        giftCardRepository.Insert(giftCard);
        return eventPublisher.EntityInsertedAsync(giftCard);
    }

    public Task UpdateGiftCardAsync(GiftCard giftCard)
    {
        ArgumentNullException.ThrowIfNull(giftCard);
        giftCardRepository.Update(giftCard);
        return eventPublisher.EntityUpdatedAsync(giftCard);
    }

    public Task<IList<GiftCard>> GetGiftCardsByPurchasedWithOrderItemIdAsync(int purchasedWithOrderItemId)
    {
        if (purchasedWithOrderItemId == 0)
            return Task.FromResult<IList<GiftCard>>([]);

        return Task.FromResult<IList<GiftCard>>(
            giftCardRepository.Table
                .Where(gc => gc.PurchasedWithOrderItemId == purchasedWithOrderItemId)
                .OrderBy(gc => gc.Id)
                .ToList());
    }

    public async Task<IList<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(int customerId)
    {
        if (customerId == 0)
            return [];

        // coupon codes stored as XML in GenericAttribute
        var attrs = await genericAttributeService.GetAttributesForEntityAsync(customerId, "Customer");
        var couponCodesXml = attrs
            .FirstOrDefault(a => a.Key == SystemCustomerAttributeNames.GiftCardCouponCodes)?.Value;

        var couponCodes = ParseGiftCardCouponCodes(couponCodesXml);
        var result = new List<GiftCard>();

        foreach (var code in couponCodes)
        {
            var giftCards = await GetAllGiftCardsAsync(isGiftCardActivated: true, giftCardCouponCode: code);
            foreach (var gc in giftCards)
            {
                if (await IsGiftCardValidAsync(gc))
                    result.Add(gc);
            }
        }

        return result;
    }

    public string GenerateGiftCardCode() => Guid.NewGuid().ToString()[..13];

    public async Task<decimal> GetGiftCardRemainingAmountAsync(GiftCard giftCard)
    {
        ArgumentNullException.ThrowIfNull(giftCard);

        var usageHistory = await GetGiftCardUsageHistoryAsync(giftCard);
        var usedAmount = usageHistory.Sum(h => h.UsedValue);
        var remaining = giftCard.Amount - usedAmount;
        return Math.Max(remaining, 0m);
    }

    public async Task<bool> IsGiftCardValidAsync(GiftCard giftCard)
    {
        ArgumentNullException.ThrowIfNull(giftCard);
        if (!giftCard.IsGiftCardActivated)
            return false;
        return await GetGiftCardRemainingAmountAsync(giftCard) > 0m;
    }

    public Task<IList<GiftCardUsageHistory>> GetGiftCardUsageHistoryAsync(GiftCard giftCard)
    {
        ArgumentNullException.ThrowIfNull(giftCard);
        return Task.FromResult<IList<GiftCardUsageHistory>>(
            giftCardUsageHistoryRepository.Table
                .Where(h => h.GiftCardId == giftCard.Id)
                .OrderByDescending(h => h.CreatedOnUtc)
                .ToList());
    }

    public Task InsertGiftCardUsageHistoryAsync(GiftCardUsageHistory usageHistory)
    {
        ArgumentNullException.ThrowIfNull(usageHistory);
        giftCardUsageHistoryRepository.Insert(usageHistory);
        return Task.CompletedTask;
    }

    public Task DeleteGiftCardUsageHistoryAsync(GiftCardUsageHistory usageHistory)
    {
        ArgumentNullException.ThrowIfNull(usageHistory);
        giftCardUsageHistoryRepository.Delete(usageHistory);
        return Task.CompletedTask;
    }

    private static string[] ParseGiftCardCouponCodes(string? couponCodesXml)
    {
        if (string.IsNullOrEmpty(couponCodesXml))
            return [];

        try
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(couponCodesXml);
            var codes = new List<string>();
            foreach (XmlNode node in xmlDoc.SelectNodes(@"//GiftCardCouponCodes/CouponCode")!)
            {
                if (node.Attributes?["Code"] is not null)
                    codes.Add(node.Attributes["Code"]!.InnerText.Trim());
            }
            return [.. codes];
        }
        catch
        {
            return [];
        }
    }
}
