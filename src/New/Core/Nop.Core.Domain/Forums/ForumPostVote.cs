namespace Nop.Core.Domain.Forums
{

    public class ForumPostVote : BaseEntity
    {

        public int ForumPostId { get; set; }

        public int CustomerId { get; set; }

        public bool IsUp { get; set; }

        public DateTime CreatedOnUtc { get; set; }
    }
}
