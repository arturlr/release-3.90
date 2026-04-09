namespace Nop.Core.Domain.Forums
{

    public class ForumPost : BaseEntity
    {

        public int TopicId { get; set; }

        public int CustomerId { get; set; }

        public string? Text { get; set; }

        public string? IPAddress { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }

        public int VoteCount { get; set; }

    }
}
