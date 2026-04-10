using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Home;

public class CommonStatisticsModel : BaseNopModel
{
    public int NumberOfOrders { get; set; }
    public int NumberOfCustomers { get; set; }
    public int NumberOfPendingReturnRequests { get; set; }
    public int NumberOfLowStockProducts { get; set; }
}
