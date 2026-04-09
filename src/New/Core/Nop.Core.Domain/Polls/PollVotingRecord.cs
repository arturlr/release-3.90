namespace Nop.Core.Domain.Polls
{

    public class PollVotingRecord : BaseEntity
    {

        public int PollAnswerId { get; set; }

        public int CustomerId { get; set; }

        public DateTime CreatedOnUtc { get; set; }

    }
}
