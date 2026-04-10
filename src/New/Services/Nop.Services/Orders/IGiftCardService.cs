using Nop.Core;
using Nop.Core.Domain.Orders;

namespace Nop.Services.Orders;

public interface IGiftCardService
{
    Task DeleteGiftCardAsync(GiftCard giftCard);
    Task<GiftCard?> GetGiftCardByIdAsync(int giftCardId);
    Task<IPagedList<GiftCard>> GetAllGiftCardsAsync(int? purchasedWithOrderId = null, int? usedWithOrderId = null,
        DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
        bool? isGiftCardActivated = null, string? giftCardCouponCode = null,
        string? recipientName = null, int pageIndex = 0, int pageSize = int.MaxValue);
    Task InsertGiftCardAsync(GiftCard giftCard);
    Task UpdateGiftCardAsync(GiftCard giftCard);
    Task<IList<GiftCard>> GetGiftCardsByPurchasedWithOrderItemIdAsync(int purchasedWithOrderItemId);
    Task<IList<GiftCard>> GetActiveGiftCardsAppliedByCustomerAsync(int customerId);
    string GenerateGiftCardCode();
    Task<decimal> GetGiftCardRemainingAmountAsync(GiftCard giftCard);
    Task<bool> IsGiftCardValidAsync(GiftCard giftCard);

    // Usage history
    Task<IList<GiftCardUsageHistory>> GetGiftCardUsageHistoryAsync(GiftCard giftCard);
    Task InsertGiftCardUsageHistoryAsync(GiftCardUsageHistory usageHistory);
    Task DeleteGiftCardUsageHistoryAsync(GiftCardUsageHistory usageHistory);
}
