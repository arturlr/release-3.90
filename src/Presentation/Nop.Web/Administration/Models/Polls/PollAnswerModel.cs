using Nop.Admin.Validators.Polls;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Polls
{
    public partial class PollAnswerModel : BaseNopEntityModel
    {
        public int PollId { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Polls.Answers.Fields.Name")]

        public string Name { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Polls.Answers.Fields.NumberOfVotes")]
        public int NumberOfVotes { get; set; }

        [NopResourceDisplayName("Admin.ContentManagement.Polls.Answers.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

    }
}