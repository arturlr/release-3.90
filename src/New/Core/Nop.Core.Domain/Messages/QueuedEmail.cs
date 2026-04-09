namespace Nop.Core.Domain.Messages
{

    public class QueuedEmail : BaseEntity
    {

        public int PriorityId { get; set; }

        public string? From { get; set; }

        public string? FromName { get; set; }

        public string? To { get; set; }

        public string? ToName { get; set; }

        public string? ReplyTo { get; set; }

        public string? ReplyToName { get; set; }

        public string? CC { get; set; }

        public string? Bcc { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }

        public string? AttachmentFilePath { get; set; }

        public string? AttachmentFileName { get; set; }

        public int AttachedDownloadId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public DateTime? DontSendBeforeDateUtc { get; set; }

        public int SentTries { get; set; }

        public DateTime? SentOnUtc { get; set; }

        public int EmailAccountId { get; set; }
        public QueuedEmailPriority Priority
        {
            get
            {
                return (QueuedEmailPriority)PriorityId;
            }
            set
            {
                PriorityId = (int)value;
            }
        }

    }
}
