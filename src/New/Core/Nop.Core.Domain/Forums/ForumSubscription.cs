namespace Nop.Core.Domain.Forums
{

    public class ForumSubscription : BaseEntity
    {

        public Guid SubscriptionGuid { get; set; }

        public int CustomerId { get; set; }

        public int ForumId { get; set; }

        public int TopicId { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
