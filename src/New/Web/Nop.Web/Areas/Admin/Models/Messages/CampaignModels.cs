using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Messages;

public class CampaignListModel : BaseNopModel
{
    public int SearchStoreId { get; set; }
    public IList<SelectListItem> AvailableStores { get; set; } = [];
}

public class CampaignModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public int StoreId { get; set; }
    public int CustomerRoleId { get; set; }
    public int EmailAccountId { get; set; }
    public DateTime? DontSendBeforeDate { get; set; }
    public string? AllowedTokens { get; set; }
    public string? TestEmail { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; } = [];
    public IList<SelectListItem> AvailableCustomerRoles { get; set; } = [];
    public IList<SelectListItem> AvailableEmailAccounts { get; set; } = [];
}

public class CampaignGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Subject { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? DontSendBeforeDate { get; set; }
}
