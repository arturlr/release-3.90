using System.Collections.Generic;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nop.Web.Models.Boards
{
    public partial class TopicMoveModel : BaseNopEntityModel
    {
        public TopicMoveModel()
        {
            ForumList = new List<SelectListItem>();
        }

        public int ForumSelected { get; set; }
        public string TopicSeName { get; set; }

        public IEnumerable<SelectListItem> ForumList { get; set; }
    }
}