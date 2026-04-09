namespace Nop.Core.Domain.Logging
{

    public class ActivityLogType : BaseEntity
    {

        public string? SystemKeyword { get; set; }

        public string? Name { get; set; }

        public bool Enabled { get; set; }
    }
}
