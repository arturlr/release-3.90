using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Messages;

public class NewsLetterSubscriptionListModel : BaseNopModel
{
    public string? SearchEmail { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int StoreId { get; set; }
    public int ActiveId { get; set; }
    public int CustomerRoleId { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = [];
    public IList<SelectListItem> ActiveList { get; set; } = [];
    public IList<SelectListItem> AvailableCustomerRoles { get; set; } = [];
}

public class NewsLetterSubscriptionModel : BaseNopEntityModel
{
    public string? Email { get; set; }
    public bool Active { get; set; }
    public string? StoreName { get; set; }
    public DateTime CreatedOn { get; set; }
}
