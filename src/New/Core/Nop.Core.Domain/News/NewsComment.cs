namespace Nop.Core.Domain.News
{

    public class NewsComment : BaseEntity
    {

        public string? CommentTitle { get; set; }

        public string? CommentText { get; set; }

        public int NewsItemId { get; set; }

        public int CustomerId { get; set; }

        public bool IsApproved { get; set; }

        public int StoreId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

    }
}
