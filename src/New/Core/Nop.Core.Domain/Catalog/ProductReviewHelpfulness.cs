namespace Nop.Core.Domain.Catalog
{

    public class ProductReviewHelpfulness : BaseEntity
    {

        public int ProductReviewId { get; set; }

        public bool WasHelpful { get; set; }

        public int CustomerId { get; set; }
    }
}
