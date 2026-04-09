namespace Nop.Core.Domain.Polls
{

    public class Poll : BaseEntity
    {
        public int LanguageId { get; set; }

        public string? Name { get; set; }

        public string? SystemKeyword { get; set; }

        public bool Published { get; set; }

        public bool ShowOnHomePage { get; set; }

        public bool AllowGuestsToVote { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime? StartDateUtc { get; set; }

        public DateTime? EndDateUtc { get; set; }
    }
}
