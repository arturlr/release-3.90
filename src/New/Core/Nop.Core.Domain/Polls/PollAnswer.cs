namespace Nop.Core.Domain.Polls
{

    public class PollAnswer : BaseEntity
    {
        public int PollId { get; set; }

        public string? Name { get; set; }

        public int NumberOfVotes { get; set; }

        public int DisplayOrder { get; set; }
    }
}
