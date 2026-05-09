using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Common
{
    public partial class UrlRecordListModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.System.SeNames.Name")]

        public string SeName { get; set; }
    }
}