using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Polls;

public class PollModel : BaseNopEntityModel
{
    public int LanguageId { get; set; }
    public IList<SelectListItem> AvailableLanguages { get; set; } = [];
    public string? LanguageName { get; set; }
    public string? Name { get; set; }
    public string? SystemKeyword { get; set; }
    public bool Published { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool AllowGuestsToVote { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class PollGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? LanguageName { get; set; }
    public int DisplayOrder { get; set; }
    public bool Published { get; set; }
    public bool ShowOnHomePage { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class PollAnswerModel : BaseNopEntityModel
{
    public int PollId { get; set; }
    public string? Name { get; set; }
    public int NumberOfVotes { get; set; }
    public int DisplayOrder { get; set; }
}
