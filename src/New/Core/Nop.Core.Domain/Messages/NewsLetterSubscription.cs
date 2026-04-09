namespace Nop.Core.Domain.Messages
{

    public class NewsLetterSubscription : BaseEntity
    {

        public Guid NewsLetterSubscriptionGuid { get; set; }

        public string? Email { get; set; }

        public bool Active { get; set; }

        public int StoreId { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
