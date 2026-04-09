namespace Nop.Core.Domain.Forums
{

    public class ForumGroup : BaseEntity
    {
        public string? Name { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }
    }
}
