namespace Nop.Core.Domain.Common
{

    public class SearchTerm : BaseEntity
    {

        public string? Keyword { get; set; }

        public int StoreId { get; set; }

        public int Count { get; set; }
    }
}
