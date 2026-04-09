namespace Nop.Core.Domain.Catalog
{

    public class ProductReview : BaseEntity
    {
        public int CustomerId { get; set; }

        public int ProductId { get; set; }

        public int StoreId { get; set; }

        public bool IsApproved { get; set; }

        public string? Title { get; set; }

        public string? ReviewText { get; set; }

        public string? ReplyText { get; set; }

        public int Rating { get; set; }

        public int HelpfulYesTotal { get; set; }

        public int HelpfulNoTotal { get; set; }

        public DateTime CreatedOnUtc { get; set; }
}
}
