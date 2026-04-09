namespace Nop.Core.Domain.Forums
{

    public class PrivateMessage : BaseEntity
    {

        public int StoreId { get; set; }

        public int FromCustomerId { get; set; }

        public int ToCustomerId { get; set; }

        public string? Subject { get; set; }

        public string? Text { get; set; }

        public bool IsRead { get; set; }

        public bool IsDeletedByAuthor { get; set; }

        public bool IsDeletedByRecipient { get; set; }

        public DateTime CreatedOnUtc { get; set; }

    }
}
