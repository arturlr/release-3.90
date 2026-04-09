using Nop.Core.Domain.Catalog;

namespace Nop.Core.Domain.Orders
{

    public class GiftCard : BaseEntity
    {
        public int? PurchasedWithOrderItemId { get; set; }

        public int GiftCardTypeId { get; set; }

        public decimal Amount { get; set; }

        public bool IsGiftCardActivated { get; set; }

        public string? GiftCardCouponCode { get; set; }

        public string? RecipientName { get; set; }

        public string? RecipientEmail { get; set; }

        public string? SenderName { get; set; }

        public string? SenderEmail { get; set; }

        public string? Message { get; set; }

        public bool IsRecipientNotified { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public GiftCardType GiftCardType
        {
            get
            {
                return (GiftCardType)GiftCardTypeId;
            }
            set
            {
                GiftCardTypeId = (int)value;
            }
        }
    }
}
