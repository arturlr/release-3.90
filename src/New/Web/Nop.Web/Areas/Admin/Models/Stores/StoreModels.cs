using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc;

namespace Nop.Web.Areas.Admin.Models.Stores;

public class StoreModel : BaseNopEntityModel
{
    public string? Name { get; set; }
    public string? Url { get; set; }
    public bool SslEnabled { get; set; }
    public string? SecureUrl { get; set; }
    public string? Hosts { get; set; }
    public int DefaultLanguageId { get; set; }
    public int DisplayOrder { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhoneNumber { get; set; }
    public string? CompanyVat { get; set; }
    public IList<SelectListItem> AvailableLanguages { get; set; } = [];
}

public class StoreGridModel
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Url { get; set; }
    public string? Hosts { get; set; }
    public int DisplayOrder { get; set; }
}
