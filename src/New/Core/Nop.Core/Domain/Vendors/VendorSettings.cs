using Nop.Core.Configuration;

namespace Nop.Core.Domain.Vendors;

public class VendorSettings : ISettings
{
    public string? DefaultVendorPageSizeOptions { get; set; }
    public int VendorsBlockItemsToDisplay { get; set; }
    public bool ShowVendorOnProductDetailsPage { get; set; }
    public bool AllowCustomersToContactVendors { get; set; }
    public bool AllowCustomersToApplyForVendorAccount { get; set; }
    public bool AllowSearchByVendor { get; set; }
    public bool AllowVendorsToEditInfo { get; set; }
    public bool NotifyStoreOwnerAboutVendorInformationChange { get; set; }
    public int MaximumProductNumber { get; set; }
    public bool AllowVendorsToImportProducts { get; set; }
}
