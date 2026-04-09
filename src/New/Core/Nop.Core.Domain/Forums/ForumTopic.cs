namespace Nop.Core.Domain.Forums
{

    public class ForumTopic : BaseEntity
    {

        public int ForumId { get; set; }

        public int CustomerId { get; set; }

        public int TopicTypeId { get; set; }

        public string? Subject { get; set; }

        public int NumPosts { get; set; }

        public int Views { get; set; }

        public int LastPostId { get; set; }

        public int LastPostCustomerId { get; set; }

        public DateTime? LastPostTime { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime UpdatedOnUtc { get; set; }

        public ForumTopicType ForumTopicType
        {
            get
            {
                return (ForumTopicType)TopicTypeId;
            }
            set
            {
                TopicTypeId = (int)value;
            }
        }

        public int NumReplies
        {
            get
            {
                int result = 0;
                if (NumPosts > 0)
                    result = NumPosts - 1;
                return result;
            }
        }
    }
}
