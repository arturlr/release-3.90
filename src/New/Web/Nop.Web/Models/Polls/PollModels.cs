using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Polls;

public class PollModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public bool AlreadyVoted { get; set; }
    public int TotalVotes { get; set; }
    public List<PollAnswerModel> Answers { get; set; } = [];
}

public class PollAnswerModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public int NumberOfVotes { get; set; }
    public double PercentOfTotalVotes { get; set; }
}
