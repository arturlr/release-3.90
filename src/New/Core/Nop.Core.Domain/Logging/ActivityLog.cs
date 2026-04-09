namespace Nop.Core.Domain.Logging
{

    public class ActivityLog : BaseEntity
    {

        public int ActivityLogTypeId { get; set; }

        public int CustomerId { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public string? IpAddress { get; set; }

    }
}
