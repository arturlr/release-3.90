namespace Nop.Core.Domain.Tasks
{

    public class ScheduleTask : BaseEntity
    {

        public string? Name { get; set; }

        public int Seconds { get; set; }

        public string? Type { get; set; }

        public bool Enabled { get; set; }

        public bool StopOnError { get; set; }

        public string? LeasedByMachineName { get; set; }

        public DateTime? LeasedUntilUtc { get; set; }

        public DateTime? LastStartUtc { get; set; }

        public DateTime? LastEndUtc { get; set; }

        public DateTime? LastSuccessUtc { get; set; }
    }
}
