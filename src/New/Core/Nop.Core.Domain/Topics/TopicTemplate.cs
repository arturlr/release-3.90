namespace Nop.Core.Domain.Topics
{

    public class TopicTemplate : BaseEntity
    {

        public string? Name { get; set; }

        public string? ViewPath { get; set; }

        public int DisplayOrder { get; set; }
    }
}
