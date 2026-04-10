using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Home;

public class DashboardModel : BaseNopModel
{
    public bool IsLoggedInAsVendor { get; set; }
}
