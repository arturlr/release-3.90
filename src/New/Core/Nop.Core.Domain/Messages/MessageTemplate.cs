using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Stores;

namespace Nop.Core.Domain.Messages
{

    public class MessageTemplate : BaseEntity, ILocalizedEntity, IStoreMappingSupported
    {

        public string? Name { get; set; }

        public string? BccEmailAddresses { get; set; }

        public string? Subject { get; set; }

        public string? Body { get; set; }

        public bool IsActive { get; set; }

        public int? DelayBeforeSend { get; set; }

        public int DelayPeriodId { get; set; }

        public int AttachedDownloadId { get; set; }

        public int EmailAccountId { get; set; }

        public bool LimitedToStores { get; set; }

        public MessageDelayPeriod DelayPeriod
        {
            get { return (MessageDelayPeriod)DelayPeriodId; }
            set { DelayPeriodId = (int)value; }
        }
    }
}
