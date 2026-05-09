using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Microsoft.AspNetCore.Mvc;


namespace Nop.Admin.Models.Vendors
{
    public partial class VendorListModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.Vendors.List.SearchName")]

        public string SearchName { get; set; }
    }
}