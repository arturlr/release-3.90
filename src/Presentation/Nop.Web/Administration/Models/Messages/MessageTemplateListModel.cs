using System.Collections.Generic;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Rendering;


namespace Nop.Admin.Models.Messages
{
    public partial class MessageTemplateListModel : BaseNopModel
    {
        public MessageTemplateListModel()
        {
            AvailableStores = new List<SelectListItem>();
        }

        [NopResourceDisplayName("Admin.ContentManagement.MessageTemplates.List.SearchStore")]
        public int SearchStoreId { get; set; }
        public IList<SelectListItem> AvailableStores { get; set; }
    }
}