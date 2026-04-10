using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Vendor;

public class ApplyVendorModel : BaseNopModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public bool DisableFormInput { get; set; }
    public string? Result { get; set; }
}

public class VendorInfoModel : BaseNopModel
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public string? PictureUrl { get; set; }
}
